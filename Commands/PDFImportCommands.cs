using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Windows.Forms;
using System.IO;
using AutoCAD_PDFImport_Project.Helpers;
using AutoCAD_PDFImport_Project.Models;
using System.Collections.Generic;
using System.Threading;

namespace AutoCAD_PDFImport_Project.Commands
{
    /// <summary>
    /// Extracts sheet info from a PDF filename by splitting at the first " - ".
    /// If not found, the entire filename is used as both SheetNumber and SheetName.
    /// </summary>
    public static class PDFSheetExtractor
    {
        public static SheetInfo ExtractSheetInfo(string pdfPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(pdfPath);
            int idx = fileName.IndexOf(" - ");
            if (idx > 0)
            {
                string sheetNo = fileName.Substring(0, idx).Trim();
                string sheetName = fileName.Substring(idx + 3).Trim();
                return new SheetInfo { SheetNumber = sheetNo, SheetName = sheetName };
            }
            else
            {
                return new SheetInfo { SheetNumber = fileName, SheetName = fileName };
            }
        }
    }

    public class PDFImportCommands
    {
        // Instance fields.
        private string _currentPdfFile = "";
        private Document _doc = null!;
        private Editor _ed = null!;
        private Database _db = null!;
        // Predefined insertion point for PDF import.
        // (Note: Ensure your drawing’s units match the expected values.)
        private readonly Point3d _insertionPoint = new Point3d(-1.0, -2.501, 0);

        [CommandMethod("ImportSelectedPDF")]
        public void ImportSelectedPDF()
        {
            _doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (_doc == null)
            {
                AcadApp.ShowAlertDialog("No active document. Open a title block drawing first.");
                return;
            }
            _ed = _doc.Editor;
            _db = _doc.Database;

            // Prompt the user to select a PDF file.
            string pdfFile = "";
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "PDF Files (*.pdf)|*.pdf";
                ofd.Title = "Select a PDF file to import";
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    _ed.WriteMessage("\nPDF selection cancelled.");
                    return;
                }
                pdfFile = ofd.FileName;
            }
            _currentPdfFile = pdfFile;
            _ed.WriteMessage($"\nProcessing PDF: {Path.GetFileName(pdfFile)}");

            // Subscribe to the CommandEnded event to update the title block after –PDFIMPORT completes.
            _doc.CommandEnded += Document_CommandEnded;

            // Send the –PDFIMPORT commands.
            SendPdfImportCommand(pdfFile);
        }

        /// <summary>
        /// Sends –PDFIMPORT inputs step-by-step with delays.
        /// We use hard-coded strings for the insertion point, scale, and rotation.
        /// </summary>
        private void SendPdfImportCommand(string pdfFile)
        {
            // Use fixed strings to ensure proper format.
            // (Adjust these values if needed.)
            string insertionPointStr = "-1,-2.501,0";
            string scaleStr = "1.0";
            string rotationStr = "0.0";

            // 1. Start the -PDFIMPORT command.
            _doc.SendStringToExecute("-PDFIMPORT\n", true, false, false);
            Thread.Sleep(1500);

            // 2. Select the "File" option.
            _doc.SendStringToExecute("F\n", true, false, false);
            Thread.Sleep(1500);

            // 3. Supply the PDF file path (in quotes).
            _doc.SendStringToExecute($"\"{pdfFile}\"\n", true, false, false);
            Thread.Sleep(1500);

            // 4. Supply the page number (first response).
            _doc.SendStringToExecute("1\n", true, false, false);
            Thread.Sleep(1500);

            // 5. Supply the page number (second response, if needed).
            _doc.SendStringToExecute("1\n", true, false, false);
            Thread.Sleep(1500);

            // 6. Supply the insertion point.
            _doc.SendStringToExecute(insertionPointStr + "\n", true, false, false);
            Thread.Sleep(1500);

            // 7. Supply the scale factor.
            _doc.SendStringToExecute(scaleStr + "\n", true, false, false);
            Thread.Sleep(1500);

            // 8. Supply the rotation.
            // Send the rotation value without an immediate newline.
            _doc.SendStringToExecute(rotationStr, true, false, false);
            Thread.Sleep(1500);

            // 9. Now send a newline to complete the input.
            _doc.SendStringToExecute("\n", true, false, false);
            Thread.Sleep(2000);
        }

        /// <summary>
        /// When AutoCAD finishes the –PDFIMPORT command, switch to the "TB" layout
        /// and update the title block MText with the sheet number and sheet name.
        /// </summary>
        private void Document_CommandEnded(object sender, CommandEventArgs e)
        {
            if (e.GlobalCommandName.Equals("PDFIMPORT", System.StringComparison.OrdinalIgnoreCase))
            {
                // Switch to the TB layout.
                SwitchToLayout("TB", _ed);

                // Extract sheet info from the PDF filename.
                SheetInfo info = PDFSheetExtractor.ExtractSheetInfo(_currentPdfFile);

                // Update title block MText objects.
                UpdateTitleBlock(_db, info, _ed);

                // Unsubscribe from the event so this runs only once.
                _doc.CommandEnded -= Document_CommandEnded;
            }
        }

        /// <summary>
        /// Switches to the specified layout.
        /// </summary>
        private void SwitchToLayout(string layoutName, Editor ed)
        {
            try
            {
                LayoutManager.Current.CurrentLayout = layoutName;
                ed.WriteMessage($"\nSwitched to layout: {layoutName}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError switching layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates MText on the "WEI-TitleBlock" layer that contain placeholders "SHEET_NO" and "SHEET_NAME"
        /// with the extracted sheet number and sheet name.
        /// </summary>
        private void UpdateTitleBlock(Database db, SheetInfo info, Editor ed)
        {
            const string titleBlockLayer = "WEI-TitleBlock";
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                    Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                    BlockTableRecord layoutBtr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);

                    foreach (ObjectId objId in layoutBtr)
                    {
                        DBObject dbObj = tr.GetObject(objId, OpenMode.ForRead);
                        if (dbObj is MText mText && mText.Layer.Equals(titleBlockLayer, System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (mText.Contents.Contains("SHEET_NO", System.StringComparison.OrdinalIgnoreCase))
                            {
                                mText.UpgradeOpen();
                                mText.Contents = info.SheetNumber ?? "NO_NUMBER";
                            }
                            if (mText.Contents.Contains("SHEET_NAME", System.StringComparison.OrdinalIgnoreCase))
                            {
                                mText.UpgradeOpen();
                                mText.Contents = info.SheetName ?? "NO_NAME";
                            }
                        }
                    }
                    tr.Commit();
                    ed.WriteMessage("\nTitle block updated successfully.");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage($"\nError updating title block: {ex.Message}");
            }
        }
    }
}
