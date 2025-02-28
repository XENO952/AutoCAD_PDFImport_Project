using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoCAD_PDFImport_Project.Commands
{
    public class LLMCommands
    {
        // This attribute registers the command "RefineTitleBlock" in AutoCAD.
        [CommandMethod("RefineTitleBlock")]
        public async void RefineTitleBlock()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            Editor ed = doc.Editor;

            // Prompt the user to enter the current title block text.
            PromptStringOptions promptOptions = new PromptStringOptions("\nEnter current title block text: ");
            promptOptions.AllowSpaces = true;
            PromptResult promptResult = ed.GetString(promptOptions);
            if (promptResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nCommand cancelled.");
                return;
            }
            if (string.IsNullOrWhiteSpace(promptResult.StringResult))
            {
                ed.WriteMessage("\nNo text provided.");
                return;
            }
            string currentText = promptResult.StringResult ?? string.Empty;

            // Prepare the payload as an anonymous object.
            var payload = new { message = currentText };

            try
            {
                // Create an HttpClient instance.
                using (HttpClient client = new HttpClient())
                {
                    // Set your API URL. Adjust the URL if needed.
                    string apiUrl = "http://127.0.0.1:5000/api/chat";
                    
                    // Post the payload as JSON.
                    HttpResponseMessage response = await client.PostAsJsonAsync(apiUrl, payload);
                    response.EnsureSuccessStatusCode();

                    // Deserialize the JSON response.
                    ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
                    string refinedText = responseData?.response ?? "No response received";

                    // Output the refined text to the AutoCAD command line.
                    ed.WriteMessage($"\nRefined title block text: {refinedText}");
                }
            }
            catch (AcadException ex)
            {
                ed.WriteMessage($"\nError calling LLM API: {ex.Message}");
            }
        }

        // Remove duplicate command definitions if they are already defined in PDFImportCommands.cs
    }

    // This class matches the JSON structure expected from your Flask API.
    public class ResponseData
    {
        public string response { get; set; } = "";
    }
}
