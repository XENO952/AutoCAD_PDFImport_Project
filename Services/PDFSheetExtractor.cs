using AutoCAD_PDFImport_Project.Helpers;
using AutoCAD_PDFImport_Project.Models;

namespace AutoCAD_PDFImport_Project.Services
{
    public static class PDFSheetExtractor
    {
        public static SheetInfo ExtractSheetInfo(string pdfPath)
        {
            // Dummy extraction logic
            var initialInfo = new SheetInfo
            {
                SheetNumber = "InitialSheetNo",
                SheetName = "InitialSheetName"
            };

            // Use local LLM to refine the sheet info
            var refinedInfo = LocalLLMRefinement.RefineSheetInfo(pdfPath, initialInfo);
            return refinedInfo;
        }
    }
}
