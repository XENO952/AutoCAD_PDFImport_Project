using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using Autodesk.AutoCAD.ApplicationServices;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class SaveDrawingCommands
    {
        [CommandMethod("SaveDrawingWithImportedPDFName")]
        public void SaveDrawingWithImportedPDFName()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                AcadApp.ShowAlertDialog("No open document. Cannot save.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;
            string baseName = string.Empty;
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    // Get each entity from model space.
                    Entity? ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent is BlockReference br)
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                        // Ensure btr.Name is not null before checking
                        if (!string.IsNullOrEmpty(btr.Name) &&
                            btr.Name.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            baseName = btr.Name;
                            break;
                        }
                    }
                }
                tr.Commit();
            }
            
            if (string.IsNullOrEmpty(baseName))
            {
                PromptStringOptions pso = new PromptStringOptions("\nImported PDF block not found. Enter base name: ");
                pso.AllowSpaces = true;
                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                    return;
                // Use the result or fallback to an empty string.
                baseName = pr.StringResult ?? string.Empty;
            }
            else
            {
                ed.WriteMessage($"\nUsing imported PDF block name: {baseName}");
            }
            
            // Remove any invalid file name characters.
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(c.ToString(), "");
            }
            
            string fileName = baseName + ".dwg";
            string folder = "";
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select folder to save the drawing:";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    folder = fbd.SelectedPath;
                else
                {
                    ed.WriteMessage("\nOperation cancelled.");
                    return;
                }
            }
            
            string fullPath = Path.Combine(folder, fileName);
            
            try
            {
                db.SaveAs(fullPath, DwgVersion.Current);
                ed.WriteMessage($"\nDrawing saved successfully as: {fullPath}");
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError saving drawing: {ex.Message}");
            }
        }

        [CommandMethod("SaveDrawingWithSheetInfo")]
        public void SaveDrawingWithSheetInfo()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                AcadApp.ShowAlertDialog("No active AutoCAD document found.");
                return;
            }

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
                        // Safely get text, even if null.
                        string textStr = dbText.TextString ?? string.Empty;
                        string txt = textStr.Trim().ToLower();
                        if (txt.Contains("sheet_no"))
                            sheetNo = textStr;
                        else if (txt.Contains("sheet_title"))
                            sheetTitle = textStr;
                    }
                    else if (obj is MText mText)
                    {
                        string contents = mText.Contents ?? string.Empty;
                        string txt = contents.Trim().ToLower();
                        if (txt.Contains("sheet_no"))
                            sheetNo = contents;
                        else if (txt.Contains("sheet_title"))
                            sheetTitle = contents;
                    }
                }
                tr.Commit();
            }
            
            if (string.IsNullOrEmpty(sheetNo))
            {
                PromptStringOptions pso = new PromptStringOptions("\nEnter sheet number: ");
                pso.AllowSpaces = false;
                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK) return;
                sheetNo = pr.StringResult ?? string.Empty;
            }
            if (string.IsNullOrEmpty(sheetTitle))
            {
                PromptStringOptions pso = new PromptStringOptions("\nEnter sheet title: ");
                pso.AllowSpaces = true;
                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK) return;
                sheetTitle = pr.StringResult ?? string.Empty;
            }
            
            string folder = "";
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select folder to save the drawing:";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    folder = fbd.SelectedPath;
                else
                {
                    ed.WriteMessage("\nOperation cancelled.");
                    return;
                }
            }
            
            // Compose file name from sheet number and title.
            string fileName = Path.Combine(folder, $"{sheetNo}_{sheetTitle}.dwg");
            try
            {
                db.SaveAs(fileName, DwgVersion.Current);
                ed.WriteMessage($"\nDrawing saved successfully as: {fileName}");
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError saving drawing: {ex.Message}");
            }
        }

        [CommandMethod("SaveDrawing")]
        public void SaveDrawing()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                AcadApp.ShowAlertDialog("No active AutoCAD document found.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;
            string? fileName = GetFileName();
            // If GetFileName returns null, use a default file name.
            SaveDrawing(db, fileName ?? "default_output.dwg", ed);
        }

        private void SaveDrawing(Database db, string fileName, Editor ed)
        {
            try
            {
                db.SaveAs(fileName, DwgVersion.Current);
                ed.WriteMessage($"\nDrawing saved successfully as: {fileName}");
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError saving drawing: {ex.Message}");
            }
        }

        public void SaveDrawing(string fileName)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                AcadApp.ShowAlertDialog("No active AutoCAD document found.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;
            try
            {
                db.SaveAs(fileName, DwgVersion.Current);
                ed.WriteMessage($"\nDrawing saved successfully as: {fileName}");
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError saving drawing: {ex.Message}");
            }
        }

        private string? GetFileName()
        {
            // TODO: Replace this stub with your actual logic to generate or retrieve a file name.
            return "default_output.dwg"; // Default file name
        }
    }
}
