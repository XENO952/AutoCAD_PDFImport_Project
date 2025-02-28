using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application; // Add this line

namespace AutoCAD_PDFImport_Project.Commands
{
    public class MyCommands
    {
        [CommandMethod("HelloWorld")]
        public void HelloWorld()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ed.WriteMessage("\nHello from AutoCAD plugin!");
        }
    }
}
