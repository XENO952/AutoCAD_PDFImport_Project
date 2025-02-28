using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using AutoCAD_PDFImport_Project.Models; // Ensure this using directive is present
using AutoCAD_PDFImport_Project.Commands;  // For SheetInfo
using Newtonsoft.Json; // Or System.Text.Json if you prefer

namespace AutoCAD_PDFImport_Project.Helpers
{
    public class LocalLLMRefinement
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public LocalLLMRefinement()
        {
            SheetNo = "";
            SheetTitle = "";
        }

        public string SheetNo { get; set; } = string.Empty;
        public string SheetTitle { get; set; } = string.Empty;

        public async Task<(string, string)> CallLocalLLMForTitleBlockAsync(string currentSheetNo, string currentSheetTitle)
        {
            try
            {
                var requestPayload = new { SheetNo = currentSheetNo, SheetTitle = currentSheetTitle };
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/llm/refineTitleBlock", requestPayload);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<LLMTitleBlockResponse>();
                return (result?.SheetNo ?? currentSheetNo, result?.SheetTitle ?? currentSheetTitle);
            }
            catch
            {
                return (currentSheetNo, currentSheetTitle);
            }
        }

        public async Task<string> CallLocalLLMForVerificationAsync(string sheetNo, string sheetTitle, Extents3d extents)
        {
            try
            {
                double width = extents.MaxPoint.X - extents.MinPoint.X;
                double height = extents.MaxPoint.Y - extents.MinPoint.Y;
                
                var requestPayload = new 
                { 
                    SheetNo = sheetNo, 
                    SheetTitle = sheetTitle,
                    Width = width,
                    Height = height
                };
                
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/llm/verify", requestPayload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return "Error: Unable to verify with LLM service";
            }
        }

        public static SheetInfo RefineSheetInfo(string pdfPath, SheetInfo initialInfo)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Replace with your actual local LLM server URL and endpoint.
                    string url = $"http://localhost:5000/refine";
                    
                    // Prepare request data. You can serialize the data into JSON if your server expects that.
                    var requestData = new
                    {
                        pdfPath = pdfPath,
                        sheetNumber = initialInfo.SheetNumber,
                        sheetName = initialInfo.SheetName
                    };
                    
                    string jsonRequest = JsonConvert.SerializeObject(requestData);
                    HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                    
                    // Synchronously wait for the result (for simplicity).
                    HttpResponseMessage response = client.PostAsync(url, content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = response.Content.ReadAsStringAsync().Result;
                        // Deserialize the JSON response into a nullable SheetInfo object.
                        SheetInfo? refinedInfo = JsonConvert.DeserializeObject<SheetInfo>(jsonResponse);
                        return refinedInfo ?? initialInfo;
                    }
                    else
                    {
                        // Log the error message (or output it to AutoCAD's command line).
                        // Fallback: return the original info.
                        return initialInfo;
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception or show a message in AutoCAD.
                // For example: ed.WriteMessage($"\nLLM refinement error: {ex.Message}");
                return initialInfo;
            }
        }

        // New method for verification.
        public static SheetInfo CallLocalLLMForVerification(SheetInfo info)
        {
            // Here, add your LLM verification logic.
            // For now, simply return the provided info.
            return info;
        }

        public string CallLocalLLMForVerification(string sheetNo, string sheetTitle, Extents3d importedExtents)
        {
            // Implementation that uses sheetNo, sheetTitle, and importedExtents
            // ...existing code...

            // Ensure a return statement is present
            return "Default return value"; // Replace with an appropriate default value or logic
        }
    }

    public class LLMTitleBlockResponse
    {
        public string SheetNo { get; set; } = "";
        public string SheetTitle { get; set; } = "";
    }
}
