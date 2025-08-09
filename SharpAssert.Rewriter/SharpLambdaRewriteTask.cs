using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
            
            // Ensure output directory exists
            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);
            
            var rewriter = new SharpAssertRewriter();
            var processedCount = 0;
            
            foreach (var sourceItem in Sources)
            {
                var sourcePath = sourceItem.ItemSpec;
                var sourceContent = File.ReadAllText(sourcePath);
                
                // Skip files that don't contain Assert calls (simple optimization)
                if (!sourceContent.Contains("Assert("))
                    continue;
                    
                var rewrittenContent = rewriter.Rewrite(sourceContent, sourcePath);
                
                // Only write file if it was actually changed
                if (rewrittenContent != sourceContent)
                {
                    var relativePath = Path.GetRelativePath(ProjectDir, sourcePath);
                    var outputPath = Path.Combine(OutputDir, relativePath + ".sharp.g.cs");
                    var outputDirPath = Path.GetDirectoryName(outputPath);
                    
                    if (!string.IsNullOrEmpty(outputDirPath) && !Directory.Exists(outputDirPath))
                        Directory.CreateDirectory(outputDirPath);
                    
                    File.WriteAllText(outputPath, rewrittenContent);
                    processedCount++;
                }
                else
                {
                    // Copy unchanged file to maintain build structure
                    var relativePath = Path.GetRelativePath(ProjectDir, sourcePath);
                    var outputPath = Path.Combine(OutputDir, relativePath + ".sharp.g.cs");
                    var outputDirPath = Path.GetDirectoryName(outputPath);
                    
                    if (!string.IsNullOrEmpty(outputDirPath) && !Directory.Exists(outputDirPath))
                        Directory.CreateDirectory(outputDirPath);
                    
                    File.WriteAllText(outputPath, sourceContent);
                }
            }
            
            Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Processed {processedCount} files with rewrites");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"SharpAssert rewriter failed: {ex.Message}");
            
            // According to PRD: graceful fallback - don't fail the build
            Log.LogWarning("SharpAssert: Falling back to original Assert behavior due to rewriter error");
            
            // Copy original files to maintain build
            foreach (var sourceItem in Sources)
            {
                try
                {
                    var sourcePath = sourceItem.ItemSpec;
                    var sourceContent = File.ReadAllText(sourcePath);
                    var relativePath = Path.GetRelativePath(ProjectDir, sourcePath);
                    var outputPath = Path.Combine(OutputDir, relativePath + ".sharp.g.cs");
                    var outputDirPath = Path.GetDirectoryName(outputPath);
                    
                    if (!string.IsNullOrEmpty(outputDirPath) && !Directory.Exists(outputDirPath))
                        Directory.CreateDirectory(outputDirPath);
                    
                    File.WriteAllText(outputPath, sourceContent);
                }
                catch
                {
                    // Even fallback failed, but don't break the build
                }
            }
            
            return true; // Return true to prevent build failure
        }
    }
}