using Microsoft.Build.Framework;

namespace SharpAssert.Rewriter;

internal enum ProcessingStatus
{
    Skipped,      // File contains no Assert calls or is a generated file
    Processed,    // File contained Assert calls and was rewritten
    Generated     // File was copied unchanged but needs to be in output
}

internal record ProcessingResult(ProcessingStatus Status, string OutputPath);

public class SharpLambdaRewriteTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Sources { get; set; } = Array.Empty<ITaskItem>();
    
    [Required]
    public string ProjectDir { get; set; } = string.Empty;
    
    [Required]
    // Not used internally but kept for compatibility with existing test/build configurations
    public string IntermediateDir { get; set; } = string.Empty;
    public string OutputDir { get; set; } = string.Empty;
    
    public string LangVersion { get; set; } = "latest";
    
    public string NullableContext { get; set; } = "enable";
    
    public bool UsePowerAssert { get; set; } = false;
    
    public bool UsePowerAssertForUnsupported { get; set; } = true;
    
    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = [];
    
    public override bool Execute()
    {
        try
        {
            return ExecuteInternal();
        }
        catch (Exception ex)
        {
            HandleExecutionError(ex);
            return true;
        }
    }

    bool ExecuteInternal()
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
            
            UpdateCounters(result.Status, ref processedCount, ref skippedCount, ref generatedFilesCount);
            AddToGeneratedFiles(result, sourcePath, generatedFiles, fileMappings);
        }

        GeneratedFiles = generatedFiles.ToArray();
        
        Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Processed {processedCount} files with rewrites");
        LogDiagnostics($"Generated {GeneratedFiles.Length} output files for MSBuild tracking");
        
        return true;
    }

    void HandleExecutionError(Exception ex)
    {
        Log.LogError($"SharpAssert rewriter failed: {ex.Message}");
        LogDiagnostics($"Exception details: {ex}");

        Log.LogWarning("SharpAssert: Falling back to original Assert behavior due to rewriter error");
        FallbackToOriginalFiles();
    }

    void UpdateCounters(ProcessingStatus status, ref int processedCount, ref int skippedCount, ref int generatedFilesCount)
    {
        switch (status)
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
    }

    void AddToGeneratedFiles(
        ProcessingResult result,
        string sourcePath,
        List<ITaskItem> generatedFiles,
        Dictionary<string, string> fileMappings)
    {
        if (string.IsNullOrEmpty(result.OutputPath))
            return;
            
        fileMappings[sourcePath] = result.OutputPath;
        generatedFiles.Add(new Microsoft.Build.Utilities.TaskItem(result.OutputPath));

        LogDiagnostics($"Mapped: {Path.GetRelativePath(ProjectDir, sourcePath)} → " +
                               $"{Path.GetRelativePath(ProjectDir, result.OutputPath)}");
    }

    static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    static bool ContainsAssertCalls(string sourceContent) => sourceContent.Contains("Assert(");

    void WriteOutputFile(string sourcePath, string content)
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
                WriteOutputFile(sourcePath, sourceContent);
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
            return ProcessSourceFileInternal(sourcePath);
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to process {sourcePath}: {ex.Message}");
            LogDiagnostics($"Processing error for {sourcePath}: {ex}");
            return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
        }
    }

    ProcessingResult ProcessSourceFileInternal(string sourcePath)
    {
        var relativePath = Path.GetRelativePath(ProjectDir, sourcePath);
        LogDiagnostics($"Processing: {relativePath}");
        
        if (IsGeneratedFile(sourcePath))
        {
            LogDiagnostics($"Skipping generated file: {relativePath}");
            return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
        }

        var sourceContent = File.ReadAllText(sourcePath);
        if (!ContainsAssertCalls(sourceContent))
        {
            LogDiagnostics($"No Assert calls found, skipping: {relativePath}");
            return new ProcessingResult(ProcessingStatus.Skipped, string.Empty);
        }

        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        LogDiagnostics($"Using absolute path for rewriter: {absoluteSourcePath}");

        var rewrittenContent = SharpAssertRewriter.Rewrite(sourceContent, absoluteSourcePath, UsePowerAssert, UsePowerAssertForUnsupported);
        if (rewrittenContent != sourceContent)
            return ProcessRewrittenContent(sourcePath, relativePath, rewrittenContent);
            
        return ProcessUnchangedContent(sourcePath, relativePath, sourceContent);
    }

    ProcessingResult ProcessRewrittenContent(string sourcePath, string relativePath, string rewrittenContent)
    {
        LogDiagnostics($"Rewrote Assert calls: {relativePath}");

        var outputPath = GetOutputPath(sourcePath);
        WriteOutputFile(sourcePath, rewrittenContent);

        return new ProcessingResult(ProcessingStatus.Processed, outputPath);
    }

    ProcessingResult ProcessUnchangedContent(string sourcePath, string relativePath, string sourceContent)
    {
        LogDiagnostics($"No rewrites needed, copying unchanged: {relativePath}");

        var outputPath = GetOutputPath(sourcePath);
        WriteOutputFile(sourcePath, sourceContent);

        return new ProcessingResult(ProcessingStatus.Processed, outputPath);
    }

    bool IsGeneratedFile(string filePath) =>
        IsGeneratedByFileName(filePath) || IsGeneratedByContent(filePath);

    bool IsGeneratedByFileName(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        return IsCommonGeneratedFile(fileName) || HasGeneratedFilePattern(fileName);
    }

    bool IsCommonGeneratedFile(string fileName) =>
        fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase) ||
        fileName.Contains("Microsoft.NET.Test.Sdk.Program.cs");

    bool HasGeneratedFilePattern(string fileName) =>
        fileName.Contains(".g.cs") || 
        fileName.Contains(".designer.") ||
        fileName.Contains(".generated.");

    bool IsGeneratedByContent(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return content.Contains("<auto-generated") || 
                   content.Contains("// <auto-generated>") ||
                   (content.Contains("#pragma warning disable") && content.Contains("generated"));
        }
        catch
        {
            return false;
        }
    }

    void LogDiagnostics(string message) =>
        // Only log diagnostic messages when MSBuild verbosity is detailed or diagnostic
        Log.LogMessage(MessageImportance.Low, $"SharpAssert: {message}");
}