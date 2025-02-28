using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AutoCAD_PDFImport_Project.Helpers; // For LocalLLMRefinement
using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class VerificationCommands
    {
        [CommandMethod("VerifyImport")]
        public void VerifyImport()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            
            string sheetNo = string.Empty;
            string sheetTitle = string.Empty;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord paperSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead);
                foreach (ObjectId id in paperSpace)
                {
                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                    if (obj is DBText dbText)
                    {
                        string txt = dbText.TextString.Trim().ToLower();
                        if (txt.Contains("sheet_no"))
                            sheetNo = dbText.TextString;
                        else if (txt.Contains("sheet_title"))
                            sheetTitle = dbText.TextString;
                    }
                    else if (obj is MText mText)
                    {
                        string txt = mText.Contents.Trim().ToLower();
                        if (txt.Contains("sheet_no"))
                            sheetNo = mText.Contents;
                        else if (txt.Contains("sheet_title"))
                            sheetTitle = mText.Contents;
                    }
                }
                tr.Commit();
            }
            
            Extents3d importedExtents = new Extents3d(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0),
                                                      new Autodesk.AutoCAD.Geometry.Point3d(36, 48, 0));
            
            // Call the helper method from LocalLLMRefinement.
            LocalLLMRefinement llm = new LocalLLMRefinement();
            string verificationSummary = llm.CallLocalLLMForVerification(sheetNo, sheetTitle, importedExtents);
            
            ed.WriteMessage("\nLLM Verification Summary:");
            ed.WriteMessage($"\n{verificationSummary}");
        }
    }
}
