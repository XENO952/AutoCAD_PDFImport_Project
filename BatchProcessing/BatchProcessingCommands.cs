using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using System.Threading;
using System.Linq;
using AutoCAD_PDFImport_Project.BatchProcessing; // For BatchProcessSettingsForm
using AutoCAD_PDFImport_Project.Commands; // For other command classes
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception; // Alias for AutoCAD exception

namespace AutoCAD_PDFImport_Project.Commands
{
    public class BatchProcessingCommands
    {
        [CommandMethod("BatchProcessPDFs")]
        public void BatchProcessPDFs()
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nBatchProcessPDFs command started.\n");

            // Prompt for folder containing PDFs.
            string folderPath = string.Empty;
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select folder containing PDF files to process:";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    folderPath = fbd.SelectedPath;
                else
                {
                    ed.WriteMessage("\nBatch processing cancelled.");
                    return;
                }
            }

            // Open UI form for batch settings.
            BatchProcessSettingsForm settingsForm = new BatchProcessSettingsForm();
            if (settingsForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                ed.WriteMessage("\nBatch processing cancelled by user.");
                return;
            }

            double customScale = settingsForm.CustomScale;
            Point3d viewportCenter = settingsForm.ViewportCenter;
            string fileNameSuffix = settingsForm.FileNameSuffix ?? string.Empty;
            // Note: TotalPages and DrawingTitle are collected but not used in this loop
            // except for updating the title block. Ensure your UpdateTitleBlockText method expects these parameters.

            // Get all PDF files from the folder.
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                ed.WriteMessage("\nNo PDF files found in the selected folder.");
                return;
            }

            // Use a for-loop so we can easily access the index.
            for (int i = 0; i < pdfFiles.Length; i++)
            {
                string pdfFile = pdfFiles[i];
                ed.WriteMessage($"\nProcessing PDF ({i + 1} of {pdfFiles.Length}): {pdfFile}");
                
                // Create a new document using the "acad.dwt" template.
                Document newDoc = AcadApp.DocumentManager.Add("acad.dwt");
                try
                {
                    newDoc.LockDocument();
                    Editor newEd = newDoc.Editor;
                    Database newDb = newDoc.Database;

                    // Import the PDF.
                    string importCmd = $".PDFIMPORT FILE \"{pdfFile}\" ";
                    newDoc.SendStringToExecute(importCmd, true, false, false);
                    newEd.WriteMessage($"\nPDFIMPORT executed for file: {pdfFile}");
                    Thread.Sleep(5000); // Wait 5 seconds for the import to complete

                    // Process the drawing:
                    // 1. Scale to exact dimensions.
                    new ScaleToExactDimensions().ScaleTo36x48();
                    newEd.WriteMessage("\nGeometry scaled to 36x48.");

                    // 2. Create viewport if not in model space.
                    if (LayoutManager.Current.CurrentLayout != "Model")
                    {
                        new ViewportCommands().CreateViewportInteractive();
                        newEd.WriteMessage("\nViewport created.");
                    }
                    else
                        newEd.WriteMessage("\nSkipping viewport creation; not in paper space.");

                    // 3. Update the title block using the settings from the form.
                    // Pass in the drawing title, current page number (i + 1), and total pages.
                    new TitleBlockTextCommands().UpdateTitleBlockText(settingsForm.DrawingTitle, i + 1, settingsForm.TotalPages);
                    newEd.WriteMessage("\nTitle block updated.");

                    // 4. Perform any verification.
                    new VerificationCommands().VerifyImport();
                    newEd.WriteMessage("\nImport verified.");

                    // Save the drawing.
                    string baseName = Path.GetFileNameWithoutExtension(pdfFile);
                    if (!string.IsNullOrEmpty(fileNameSuffix))
                        baseName += "_" + fileNameSuffix;
                    string outputFileName = baseName + ".dwg";
                    string outputPath = Path.Combine(folderPath, outputFileName);

                    newDb.SaveAs(outputPath, DwgVersion.Current);
                    newEd.WriteMessage($"\nSaved drawing as: {outputPath}");
                }
                catch (AcadException ex)
                {
                    ed.WriteMessage($"\nError processing {pdfFile}: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError processing {pdfFile}: {ex.Message}");
                }
                finally
                {
                    newDoc.CloseAndDiscard();
                }
            }

            ed.WriteMessage("\nBatch processing completed.");
        }
    }
}
