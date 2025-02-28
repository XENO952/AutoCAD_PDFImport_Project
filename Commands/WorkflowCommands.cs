using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Threading;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class WorkflowCommands
    {
        [CommandMethod("RunPDFImportWorkflow")]
        public void RunPDFImportWorkflow()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            
            string pdfFile = "";
            using (var dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Filter = "PDF Files|*.pdf";
                dlg.Title = "Select PDF File to Import";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    pdfFile = dlg.FileName;
                else
                {
                    ed.WriteMessage("\nOperation cancelled.");
                    return;
                }
            }
            
            string importCmd = $".PDFIMPORT FILE \"{pdfFile}\" ";
            doc.SendStringToExecute(importCmd, true, false, false);
            ed.WriteMessage($"\nPDFIMPORT command executed for file: {pdfFile}");
            
            Thread.Sleep(3000); // Wait for the import to complete
            
            PromptSelectionResult selRes = ed.SelectAll();
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo geometry found in model space.");
                return;
            }
            // Scale, create viewport, update title block, and verify.
            new ScaleToExactDimensions().ScaleTo36x48();
            ed.WriteMessage("\nImported geometry scaled to 36x48.");
            
            if (LayoutManager.Current.CurrentLayout == "Model")
            {
                ed.WriteMessage("\nSwitch to a paper space layout to create a viewport.");
                return;
            }
            new ViewportCommands().CreateViewportInteractive();
            new TitleBlockTextCommands().UpdateTitleBlockText();
            new VerificationCommands().VerifyImport();
            
            ed.WriteMessage("\nWorkflow completed successfully.");
        }
    }
}
