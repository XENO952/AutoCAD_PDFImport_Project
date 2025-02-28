using Autodesk.AutoCAD.Runtime;
using AutoCAD_PDFImport_Project.Commands;

// Register each class that contains commands.
[assembly: CommandClass(typeof(MyCommands))]
[assembly: CommandClass(typeof(PDFImportCommands))]
[assembly: CommandClass(typeof(ScaleToExactDimensions))]
[assembly: CommandClass(typeof(TitleBlockTextCommands))]
[assembly: CommandClass(typeof(VerificationCommands))]
[assembly: CommandClass(typeof(ViewportCommands))]
[assembly: CommandClass(typeof(WorkflowCommands))]
[assembly: CommandClass(typeof(LLMCommands))]
