using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class SharpLambdaRewriteTaskFixture
{
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        var outputFile = Path.Combine(outputDir, "Test.cs.sharp.g.cs");
        File.Exists(outputFile).Should().BeTrue();
        var content = File.ReadAllText(outputFile);
        content.Should().Contain("global::SharpInternal.Assert(()=>");
        content.Should().Contain("x == 1");
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        var outputFile = Path.Combine(outputDir, "NoAssert.cs.sharp.g.cs");
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile1), CreateTaskItem(sourceFile2) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFile1 = Path.Combine(outputDir, "Test1.cs.sharp.g.cs");
        var outputFile2 = Path.Combine(outputDir, "Test2.cs.sharp.g.cs");
        
        File.Exists(outputFile1).Should().BeTrue();
        File.Exists(outputFile2).Should().BeTrue();
        
        var content1 = File.ReadAllText(outputFile1);
        var content2 = File.ReadAllText(outputFile2);
        
        content1.Should().Contain("global::SharpInternal.Assert(()=>");
        content2.Should().Contain("global::SharpInternal.Assert(()=>");
        content2.Should().Contain("1 == 1");
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFileWithAssert), CreateTaskItem(sourceFileWithoutAssert) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var outputFileWithAssert = Path.Combine(outputDir, "WithAssert.cs.sharp.g.cs");
        var outputFileWithoutAssert = Path.Combine(outputDir, "WithoutAssert.cs.sharp.g.cs");
        
        File.Exists(outputFileWithAssert).Should().BeTrue("files with Assert calls should be rewritten");
        var contentWithAssert = File.ReadAllText(outputFileWithAssert);
        contentWithAssert.Should().Contain("global::SharpInternal.Assert(()=>");
        
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        
        var expectedOutputFile = Path.Combine(outputDir, "src", "nested", "Nested.cs.sharp.g.cs");
        File.Exists(expectedOutputFile).Should().BeTrue("output path should preserve directory structure");
        var content = File.ReadAllText(expectedOutputFile);
        content.Should().Contain("global::SharpInternal.Assert(()=>");
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        var outputFile = Path.Combine(outputDir, "AsyncAssert.cs.sharp.g.cs");
        File.Exists(outputFile).Should().BeTrue("file with async Assert should still be processed");
        
        var content = File.ReadAllText(outputFile);
        content.Should().NotContain("global::SharpInternal.Assert(()=>");
        content.Should().Contain("Assert(await GetBoolAsync())"); // Original Assert should remain
    }

    [Test]
    public void Should_handle_completely_invalid_syntax_gracefully()
    {
        var sourceFile = CreateSourceFile("InvalidSyntax.cs", """
            using static Sharp;
            
            this is not valid C# code at all!!!
            class @#$%^&*() { Assert(x == 1); }
            """);
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue("task should handle errors gracefully and return true");
        var outputFile = Path.Combine(outputDir, "InvalidSyntax.cs.sharp.g.cs");
        File.Exists(outputFile).Should().BeTrue("fallback should create output file with original content");
        
        var content = File.ReadAllText(outputFile);
        content.Should().Contain("this is not valid C# code at all!!!"); // Original content preserved in fallback
    }

    [Test]
    public void Should_handle_empty_source_list()
    {
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = Array.Empty<ITaskItem>(),
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
        };
        
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = new MockBuildEngine()
            // LangVersion and NullableContext not set - should use defaults
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        task.LangVersion.Should().Be("latest");
        task.NullableContext.Should().Be("enable");
    }

    [Test]
    public void Should_accept_custom_langversion_and_nullable_context()
    {
        var sourceFile = CreateSourceFile("CustomConfig.cs", """
            using static Sharp;
            class CustomConfig { void Method() { Assert(true); } }
            """);
        
        var outputDir = Path.Combine(tempDir, "output");
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            LangVersion = "9.0",
            NullableContext = "disable",
            BuildEngine = new MockBuildEngine()
        };
        
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
        
        var outputDir = Path.Combine(tempDir, "output");
        var mockEngine = new MockBuildEngine();
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            IntermediateDir = Path.Combine(tempDir, "obj"),
            OutputDir = outputDir,
            BuildEngine = mockEngine
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        mockEngine.Messages.Should().Contain(msg => msg.Contains("SharpAssert: Rewriting 1 source files"));
        mockEngine.Messages.Should().Contain(msg => msg.Contains("SharpAssert: Processed 1 files with rewrites"));
    }
    
    string CreateTempDirectory()
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
    
    ITaskItem CreateTaskItem(string itemSpec)
    {
        return new TaskItem(itemSpec);
    }
}

public class MockBuildEngine : IBuildEngine
{
    public List<string> Messages { get; } = new List<string>();
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();

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