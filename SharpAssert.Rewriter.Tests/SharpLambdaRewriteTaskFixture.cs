using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class SharpLambdaRewriteTaskFixture
{
    const string DefaultLangVersion = "latest";
    const string DefaultNullableContext = "enable";
    const string ExpectedRewrittenAssert = "global::SharpAssert.SharpInternal.Assert(()=>";
    
    string tempDir;
    
    [SetUp]
    public void Setup()
    {
        tempDir = CreateTempDirectory();
    }
    
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }
    
    [Test]
    public void Should_execute_successfully_with_simple_assert_file()
    {
        var sourceFile = CreateSourceFile("Test.cs", """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    Assert(x == 1); 
                } 
            }
            """);
        
        var task = CreateTask(sourceFile);
        
        var result = task.Execute();

        result.Should().BeTrue();
        
        var outputFile = GetExpectedOutputPath(sourceFile);
        VerifyRewrittenOutput(outputFile, "x == 1");
    }

    [Test]
    public void Should_copy_file_without_assert_calls_unchanged()
    {
        var sourceFile = CreateSourceFile("NoAssert.cs", """
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    Console.WriteLine("No asserts here"); 
                } 
            }
            """);
        
        var task = CreateTask(sourceFile);
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFile = GetExpectedOutputPath(sourceFile);
        File.Exists(outputFile).Should().BeFalse("files without Assert calls should not be processed");
    }

    [Test]
    public void Should_process_multiple_source_files()
    {
        var sourceFile1 = CreateSourceFile("Test1.cs", """
            using static Sharp;
            class Test1 { void Method() { Assert(true); } }
            """);
        
        var sourceFile2 = CreateSourceFile("Test2.cs", """
            using static Sharp;
            class Test2 { void Method() { Assert(1 == 1); } }
            """);
        
        var task = CreateTask(sourceFile1, sourceFile2);
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFile1 = GetExpectedOutputPath(sourceFile1);
        var outputFile2 = GetExpectedOutputPath(sourceFile2);
        
        VerifyRewrittenOutput(outputFile1);
        VerifyRewrittenOutput(outputFile2, "1 == 1");
    }

    [Test]
    public void Should_rewrite_files_with_assert_calls_and_leave_unchanged_files_unchanged()
    {
        var sourceFileWithAssert = CreateSourceFile("WithAssert.cs", """
            using static Sharp;
            class TestWithAssert { void Method() { Assert(1 == 1); } }
            """);
        
        var sourceFileWithoutAssert = CreateSourceFile("WithoutAssert.cs", """
            class TestWithoutAssert { void Method() { Console.WriteLine("test"); } }
            """);
        
        var task = CreateTask(sourceFileWithAssert, sourceFileWithoutAssert);
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFileWithAssert = GetExpectedOutputPath(sourceFileWithAssert);
        var outputFileWithoutAssert = GetExpectedOutputPath(sourceFileWithoutAssert);
        
        File.Exists(outputFileWithAssert).Should().BeTrue("files with Assert calls should be rewritten");
        var contentWithAssert = File.ReadAllText(outputFileWithAssert);
        contentWithAssert.Should().Contain("global::SharpAssert.SharpInternal.Assert(()=>");
        
        File.Exists(outputFileWithoutAssert).Should().BeFalse("files without Assert calls should be skipped");
    }

    [Test]
    public void Should_generate_correct_output_paths_for_nested_directories()
    {
        var nestedDir = Path.Combine(tempDir, "src", "nested");
        Directory.CreateDirectory(nestedDir);
        
        var sourceFile = Path.Combine(nestedDir, "Nested.cs");
        File.WriteAllText(sourceFile, """
            using static Sharp;
            class Nested { void Method() { Assert(true); } }
            """);
        
        var task = CreateTask(sourceFile);
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var expectedOutputFile = GetExpectedOutputPath(sourceFile);
        File.Exists(expectedOutputFile).Should().BeTrue("output path should preserve directory structure");
        
        var content = File.ReadAllText(expectedOutputFile);
        content.Should().Contain(ExpectedRewrittenAssert);
    }

    [Test]
    public void Should_skip_rewriting_files_with_async_assert_calls()
    {
        var sourceFile = CreateSourceFile("AsyncAssert.cs", """
            using static Sharp;
            using System.Threading.Tasks;
            
            class AsyncAssert 
            { 
                async Task Method() 
                { 
                    Assert(await GetBoolAsync()); 
                } 
                
                Task<bool> GetBoolAsync() => Task.FromResult(true);
            }
            """);
        
        var task = CreateTask(sourceFile);
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFile = GetExpectedOutputPath(sourceFile);
        File.Exists(outputFile).Should().BeTrue("file with async Assert should still be processed");
        
        var content = File.ReadAllText(outputFile);
        content.Should().NotContain(ExpectedRewrittenAssert);
        content.Should().Contain("Assert(await GetBoolAsync())",
            "Original Assert should remain");
    }

    [Test]
    public void Should_handle_completely_invalid_syntax_gracefully()
    {
        var sourceFile = CreateSourceFile("InvalidSyntax.cs", """
            using static Sharp;
            
            this is not valid C# code at all!!!
            class @#$%^&*() { Assert(x == 1); }
            """);
        
        var task = CreateTask(sourceFile);
        
        var result = task.Execute();
        
        result.Should().BeTrue("task should handle errors gracefully and return true");
        
        var outputFile = GetExpectedOutputPath(sourceFile);
        File.Exists(outputFile).Should().BeTrue("fallback should create output file with original content");
        
        var content = File.ReadAllText(outputFile);
        content.Should().Contain("this is not valid C# code at all!!!",
            "Original content preserved in fallback");
    }

    [Test]
    public void Should_handle_empty_source_list()
    {
        var task = CreateTask();
        
        var result = task.Execute();
        
        result.Should().BeTrue("empty source list should be handled gracefully");
    }

    [Test]
    public void Should_use_default_langversion_and_nullable_context()
    {
        var sourceFile = CreateSourceFile("DefaultConfig.cs", """
            using static Sharp;
            class DefaultConfig { void Method() { Assert(true); } }
            """);
        
        var task = CreateTask(sourceFile);
        // LangVersion and NullableContext not set - should use defaults
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        task.LangVersion.Should().Be(DefaultLangVersion);
        task.NullableContext.Should().Be(DefaultNullableContext);
    }

    [Test]
    public void Should_accept_custom_langversion_and_nullable_context()
    {
        var sourceFile = CreateSourceFile("CustomConfig.cs", """
            using static Sharp;
            class CustomConfig { void Method() { Assert(true); } }
            """);
        
        var task = CreateTask(sourceFile);
        task.LangVersion = "9.0";
        task.NullableContext = "disable";
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        task.LangVersion.Should().Be("9.0");
        task.NullableContext.Should().Be("disable");
    }

    [Test]
    public void Should_log_processing_messages()
    {
        var sourceFile = CreateSourceFile("LogTest.cs", """
            using static Sharp;
            class LogTest { void Method() { Assert(true); } }
            """);
        
        var task = CreateTaskWithMockEngine(sourceFile);
        var mockEngine = (MockBuildEngine)task.BuildEngine;
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        mockEngine.Messages.Should().Contain(msg => msg.Contains("SharpAssert: Rewriting 1 source files"));
        mockEngine.Messages.Should().Contain(msg => msg.Contains("SharpAssert: Processed 1 files with rewrites"));
    }

    static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "SharpAssertTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
    
    string CreateSourceFile(string fileName, string content)
    {
        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
    
    ITaskItem CreateTaskItem(string itemSpec) => new TaskItem(itemSpec);

    SharpLambdaRewriteTask CreateTask(params string[] sourceFiles) => new()
    {
        Sources = sourceFiles.Select(CreateTaskItem).ToArray(),
        ProjectDir = tempDir,
        IntermediateDir = Path.Combine(tempDir, "obj"),
        OutputDir = Path.Combine(tempDir, "output"),
        BuildEngine = new MockBuildEngine()
    };

    SharpLambdaRewriteTask CreateTaskWithMockEngine(params string[] sourceFiles)
    {
        var mockEngine = new MockBuildEngine();
        var task = CreateTask(sourceFiles);
        task.BuildEngine = mockEngine;
        return task;
    }
    
    string GetExpectedOutputPath(string sourceFile)
    {
        var relativePath = Path.GetRelativePath(tempDir, sourceFile);
        return Path.Combine(tempDir, "output", relativePath + ".sharp.g.cs");
    }
    
    void VerifyRewrittenOutput(string outputFile, string? expectedContent = null)
    {
        File.Exists(outputFile).Should().BeTrue();
        var content = File.ReadAllText(outputFile);
        content.Should().Contain(ExpectedRewrittenAssert);
        
        if (expectedContent != null)
            content.Should().Contain(expectedContent);
    }
}

public class MockBuildEngine : IBuildEngine
{
    public List<string> Messages { get; } = [];
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public bool ContinueOnError => false;
    public int LineNumberOfTaskNode => 0;
    public int ColumnNumberOfTaskNode => 0;
    public string ProjectFileOfTaskNode => "";

    public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;
    public void LogCustomEvent(CustomBuildEventArgs e) { }
    public void LogErrorEvent(BuildErrorEventArgs e) { Errors.Add(e.Message ?? ""); }
    public void LogMessageEvent(BuildMessageEventArgs e) { Messages.Add(e.Message ?? ""); }
    public void LogWarningEvent(BuildWarningEventArgs e) { Warnings.Add(e.Message ?? ""); }
}