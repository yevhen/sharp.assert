using Microsoft.Build.Framework;

namespace SharpAssert.Rewriter;

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
    
    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Rewriting {Sources.Length} source files");

            EnsureDirectoryExists(OutputDir);

            var rewriter = new SharpAssertRewriter();
            var processedCount = 0;

            foreach (var sourceItem in Sources)
            {
                var sourcePath = sourceItem.ItemSpec;
                var sourceContent = File.ReadAllText(sourcePath);

                if (!ContainsAssertCalls(sourceContent))
                    continue;

                var rewrittenContent = SharpAssertRewriter.Rewrite(sourceContent, sourcePath);

                if (rewrittenContent != sourceContent)
                {
                    WriteProcessedFile(sourcePath, rewrittenContent);
                    processedCount++;
                }
                else
                {
                    WriteUnchangedFile(sourcePath, sourceContent);
                }
            }

            Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Processed {processedCount} files with rewrites");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"SharpAssert rewriter failed: {ex.Message}");
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
}