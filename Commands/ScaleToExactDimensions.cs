using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using Autodesk.AutoCAD.ApplicationServices;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class ScaleToExactDimensions
    {
        [CommandMethod("ScaleTo36x48")]
        public void ScaleTo36x48()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument!;
            if (doc == null)
            {
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;
            
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo entities selected.");
                return;
            }
            
            // Calculate overall extents.
            Extents3d overallExtents = new Extents3d();
            bool first = true;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    Entity? ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        if (first)
                        {
                            overallExtents = ent.GeometricExtents;
                            first = false;
                        }
                        else
                        {
                            overallExtents.AddExtents(ent.GeometricExtents);
                        }
                    }
                }
                tr.Commit();
            }
            
            double currentWidth = overallExtents.MaxPoint.X - overallExtents.MinPoint.X;
            double currentHeight = overallExtents.MaxPoint.Y - overallExtents.MinPoint.Y;
            
            if (currentWidth == 0 || currentHeight == 0)
            {
                ed.WriteMessage("\nInvalid geometry dimensions for scaling.");
                return;
            }
            
            double targetWidth = 36.0;
            double targetHeight = 48.0;
            double scaleFactorX = targetWidth / currentWidth;
            double scaleFactorY = targetHeight / currentHeight;
            
            Point3d center = new Point3d(
                (overallExtents.MinPoint.X + overallExtents.MaxPoint.X) / 2.0,
                (overallExtents.MinPoint.Y + overallExtents.MaxPoint.Y) / 2.0,
                (overallExtents.MinPoint.Z + overallExtents.MaxPoint.Z) / 2.0);
            
            Matrix3d translationToOrigin = Matrix3d.Displacement(center.GetAsVector() * -1);
            double[] scaleValues = new double[16]
            {
                scaleFactorX, 0,           0, 0,
                0,           scaleFactorY, 0, 0,
                0,           0,           1, 0,
                0,           0,           0, 1
            };
            Matrix3d nonUniformScaling = new Matrix3d(scaleValues);
            Matrix3d translationBack = Matrix3d.Displacement(center.GetAsVector());
            Matrix3d finalTransform = translationBack * nonUniformScaling * translationToOrigin;
            
            double scale = GetScale();
            if (scale == 0)
            {
                scale = 1.0; // Fallback
            }
            if (double.IsNaN(scale) || double.IsInfinity(scale))
            {
                ed.WriteMessage("\nInvalid scale factor.");
                return;
            }

            int successCount = 0;
            int skipCount = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    try
                    {
                        ent.TransformBy(finalTransform);
                        successCount++;
                    }
                    catch (AcadException ex)
                    {
                        if (ex.ErrorStatus == ErrorStatus.CannotScaleNonUniformly)
                        {
                            ed.WriteMessage($"\nWarning: Entity {id} cannot be scaled non-uniformly and was skipped.");
                            skipCount++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                tr.Commit();
            }
            
            ed.WriteMessage($"\nScaling complete. Successfully scaled {successCount} entities. Skipped {skipCount} entities.");
            ed.WriteMessage($"\nApplied scale factors: X = {scaleFactorX:F2}, Y = {scaleFactorY:F2}");
        }

        private double GetScale()
        {
            // Always return a valid double (e.g., from user input or a config)
            // For now, let's hardcode as 1.0
            return 1.0;
        }
    }
}
