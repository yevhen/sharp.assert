using Microsoft.Build.Framework;

namespace SharpAssert.Rewriter;

enum ProcessingStatus
{
    Skipped,      // File contains no Assert calls or is a generated file
    Processed,    // File contained Assert calls and was rewritten
    Generated     // File was copied unchanged but needs to be in output
}

record ProcessingResult(ProcessingStatus Status, string OutputPath);

public class SharpLambdaRewriteTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Sources { get; set; } = Array.Empty<ITaskItem>();
    
    [Required]
    public string ProjectDir { get; set; } = string.Empty;
    
    [Required]
    public string IntermediateDir { get; set; } = string.Empty;
    
    [Required]
    public string OutputDir { get; set; } = string.Empty;
    
    public string LangVersion { get; set; } = "latest";
    
    public string NullableContext { get; set; } = "enable";
    
    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();
    
    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Rewriting {Sources.Length} source files");
            LogDiagnostics($"Project directory: {ProjectDir}");
            LogDiagnostics($"Output directory: {OutputDir}");
            LogDiagnostics($"Language version: {LangVersion}");
            LogDiagnostics($"Nullable context: {NullableContext}");

            EnsureDirectoryExists(OutputDir);

            var generatedFiles = new List<ITaskItem>();
            var fileMappings = new Dictionary<string, string>();
            var processedCount = 0;
            var skippedCount = 0;
            var generatedFilesCount = 0;

            foreach (var sourceItem in Sources)
            {
                var sourcePath = sourceItem.ItemSpec;
                var result = ProcessSourceFile(sourcePath);
                
                switch (result.Status)
                {
                    case ProcessingStatus.Processed:
                        processedCount++;
                        break;
                    case ProcessingStatus.Skipped:
                        skippedCount++;
                        break;
                    case ProcessingStatus.Generated:
                        generatedFilesCount++;
                        break;
                }

                if (!string.IsNullOrEmpty(result.OutputPath))
                {
                    fileMappings[sourcePath] = result.OutputPath;
                    generatedFiles.Add(new Microsoft.Build.Utilities.TaskItem(result.OutputPath));
                    LogDiagnostics($"Mapped: {Path.GetRelativePath(ProjectDir, sourcePath)} → {Path.GetRelativePath(ProjectDir, result.OutputPath)}");
                }
            }

            GeneratedFiles = generatedFiles.ToArray();

            Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Processed {processedCount} files with rewrites");
            
            LogDiagnostics($"Generated {GeneratedFiles.Length} output files for MSBuild tracking");
            
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"SharpAssert rewriter failed: {ex.Message}");
            LogDiagnostics($"Exception details: {ex}");
            Log.LogWarning("SharpAssert: Falling back to original Assert behavior due to rewriter error");

            FallbackToOriginalFiles();
            return true;
        }
    }

    static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    static bool ContainsAssertCalls(string sourceContent) => sourceContent.Contains("Assert(");

    void WriteProcessedFile(string sourcePath, string content)
    {
        var outputPath = GetOutputPath(sourcePath);
        EnsureDirectoryExists(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, content);
    }

    void WriteUnchangedFile(string sourcePath, string content)
    {
        var outputPath = GetOutputPath(sourcePath);
        EnsureDirectoryExists(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, content);
    }

    string GetOutputPath(string sourcePath)
    {
        var relativePath = Path.GetRelativePath(ProjectDir, sourcePath);
        return Path.Combine(OutputDir, relativePath + ".sharp.g.cs");
    }

    void FallbackToOriginalFiles()
    {
        foreach (var sourceItem in Sources)
        {
            try
            {
                var sourcePath = sourceItem.ItemSpec;
                var sourceContent = File.ReadAllText(sourcePath);
                WriteUnchangedFile(sourcePath, sourceContent);
            }
            catch
            {
                // ignored
            }
        }
    }

    ProcessingResult ProcessSourceFile(string sourcePath)
    {
        try
        {
            LogDiagnostics($"Processing: {Path.GetRelativePath(ProjectDir, sourcePath)}");
            
            if (IsGeneratedFile(sourcePath))
            {
                LogDiagnostics($"Skipping generated file: {Path.GetRelativePath(ProjectDir, sourcePath)}");
                return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
            }

            var sourceContent = File.ReadAllText(sourcePath);
            
            if (!ContainsAssertCalls(sourceContent))
            {
                LogDiagnostics($"No Assert calls found, skipping: {Path.GetRelativePath(ProjectDir, sourcePath)}");
                return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
            }

            var rewrittenContent = SharpAssertRewriter.Rewrite(sourceContent, sourcePath);
            if (rewrittenContent != sourceContent)
            {
                LogDiagnostics($"Rewrote Assert calls: {Path.GetRelativePath(ProjectDir, sourcePath)}");
                var outputPath = GetOutputPath(sourcePath);
                WriteProcessedFile(sourcePath, rewrittenContent);
                return new ProcessingResult(ProcessingStatus.Processed, outputPath);
            }
            else
            {
                LogDiagnostics($"No rewrites needed, copying unchanged: {Path.GetRelativePath(ProjectDir, sourcePath)}");
                var outputPath = GetOutputPath(sourcePath);
                WriteUnchangedFile(sourcePath, sourceContent);
                return new ProcessingResult(ProcessingStatus.Processed, outputPath);
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to process {sourcePath}: {ex.Message}");
            LogDiagnostics($"Processing error for {sourcePath}: {ex}");
            return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
        }
    }

    bool IsGeneratedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        // Skip common generated files
        if (fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Microsoft.NET.Test.Sdk.Program.cs"))
        {
            return true;
        }

        // Skip files that look generated
        if (fileName.Contains(".g.cs") || 
            fileName.Contains(".designer.") ||
            fileName.Contains(".generated."))
        {
            return true;
        }

        // Skip if file contains generated code markers
        try
        {
            var content = File.ReadAllText(filePath);
            if (content.Contains("<auto-generated") || 
                content.Contains("// <auto-generated>") ||
                content.Contains("#pragma warning disable") && content.Contains("generated"))
            {
                return true;
            }
        }
        catch
        {
            // If we can't read the file, assume it's not generated
        }

        return false;
    }

    void LogDiagnostics(string message)
    {
        // Only log diagnostic messages when MSBuild verbosity is detailed or diagnostic
        Log.LogMessage(MessageImportance.Low, $"SharpAssert: {message}");
    }
}