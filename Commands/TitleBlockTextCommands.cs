using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class TitleBlockTextCommands
    {
        [CommandMethod("UpdateTitleBlockText", CommandFlags.UsePickSet)]
        public void UpdateTitleBlockText()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            
            PromptStringOptions psoSheetNo = new PromptStringOptions("\nEnter sheet number: ");
            psoSheetNo.AllowSpaces = false;
            PromptResult prSheetNo = ed.GetString(psoSheetNo);
            if (prSheetNo.Status != PromptStatus.OK) return;
            
            PromptStringOptions psoTitle = new PromptStringOptions("\nEnter sheet title: ");
            psoTitle.AllowSpaces = true;
            PromptResult prTitle = ed.GetString(psoTitle);
            if (prTitle.Status != PromptStatus.OK) return;
            
            string newSheetNo = prSheetNo.StringResult ?? string.Empty;
            string newSheetTitle = prTitle.StringResult ?? string.Empty;
            
            UpdateTitleBlockText(newSheetNo, newSheetTitle);
        }

        public void UpdateTitleBlockText(string sheetNo, string sheetTitle)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord paperSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite);
                    int updatedCount = 0;
                    foreach (ObjectId id in paperSpace)
                    {
                        DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
                        if (obj is DBText dbText)
                        {
                            string txt = dbText.TextString.Trim().ToLower();
                            if (txt.Contains("sheet_title"))
                            {
                                dbText.TextString = sheetTitle;
                                updatedCount++;
                            }
                            else if (txt.Contains("sheet_no"))
                            {
                                dbText.Annotative = AnnotativeStates.False;
                                dbText.TextString = sheetNo;
                                updatedCount++;
                            }
                        }
                        else if (obj is MText mText)
                        {
                            string txt = mText.Contents.Trim().ToLower();
                            if (txt.Contains("sheet_title"))
                            {
                                mText.Contents = sheetTitle;
                                updatedCount++;
                            }
                            else if (txt.Contains("sheet_no"))
                            {
                                mText.Annotative = AnnotativeStates.False;
                                mText.Contents = sheetNo;
                                updatedCount++;
                            }
                        }
                    }
                    tr.Commit();
                    ed.WriteMessage($"\nUpdated {updatedCount} title block text objects.");
                }
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError updating title block text: {ex.Message}");
            }
        }

        public void UpdateTitleBlockText(string title, int currentPage, int totalPages)
        {
            // Implementation here
        }
    }
}
