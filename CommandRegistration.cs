using Autodesk.AutoCAD.Runtime;
using AutoCAD_PDFImport_Project.Commands; // Adjust if your namespace is different

[assembly: CommandClass(typeof(MyCommands))]
[assembly: CommandClass(typeof(PDFImportCommands))]
[assembly: CommandClass(typeof(ScaleToExactDimensions))]
[assembly: CommandClass(typeof(TitleBlockTextCommands))]
[assembly: CommandClass(typeof(VerificationCommands))]
[assembly: CommandClass(typeof(ViewportCommands))]
[assembly: CommandClass(typeof(WorkflowCommands))]
[assembly: CommandClass(typeof(LLMCommands))]
[assembly: CommandClass(typeof(BatchProcessingCommands))]
