using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class ViewportCommands
    {
        [CommandMethod("CreateViewportInteractive")]
        public void CreateViewportInteractive()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            
            PromptPointResult prLower = ed.GetPoint("\nSelect the lower-left corner of the viewport: ");
            if (prLower.Status != PromptStatus.OK) return;
            Point3d lowerLeft = prLower.Value;
            
            PromptPointResult prUpper = ed.GetPoint("\nSelect the upper-right corner of the viewport: ");
            if (prUpper.Status != PromptStatus.OK) return;
            Point3d upperRight = prUpper.Value;
            
            double viewportWidth = Math.Abs(upperRight.X - lowerLeft.X);
            double viewportHeight = Math.Abs(upperRight.Y - lowerLeft.Y);
            double centerX = (lowerLeft.X + upperRight.X) / 2.0;
            double centerY = (lowerLeft.Y + upperRight.Y) / 2.0;
            Point3d viewportCenter = new Point3d(centerX, centerY, 0);
            
            double customScale = 0.818355;  // Example scale
            
            PromptPointResult prModelCenter = ed.GetPoint("\nSelect the model-space center for the viewport: ");
            if (prModelCenter.Status != PromptStatus.OK) return;
            Point2d modelSpaceCenter = new Point2d(prModelCenter.Value.X, prModelCenter.Value.Y);
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayoutManager lm = LayoutManager.Current;
                if (lm.CurrentLayout == "Model")
                {
                    ed.WriteMessage("\nSwitch to a paper space layout before creating a viewport.");
                    return;
                }
                
                ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
                Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);
                BlockTableRecord paperSpace = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);
                
                Viewport vp = new Viewport();
                vp.SetDatabaseDefaults();
                vp.CenterPoint = viewportCenter;
                vp.Width = viewportWidth;
                vp.Height = viewportHeight;
                vp.CustomScale = customScale;
                vp.ViewCenter = modelSpaceCenter;
                
                paperSpace.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);
                
                // Ensure the viewport is turned on.
                ObjectId vpId = vp.ObjectId;
                Viewport vpDb = (Viewport)tr.GetObject(vpId, OpenMode.ForWrite);
                vpDb.On = true;
                
                tr.Commit();
            }
            
            ed.WriteMessage("\nViewport created interactively.");
        }
    }
}
