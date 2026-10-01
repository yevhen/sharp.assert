using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpAssert;

static class RewrittenSource
{
    public static CSharpCompilation Compile(string source)
    {
        var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");
        var references = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "CompilationReferences"), "*.dll")
            .Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create(Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(rewritten)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    public static Action LoadCheck(string source)
    {
        using var output = new MemoryStream();
        var result = Compile(source).Emit(output);
        result.Diagnostics.Where(each => each.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().BeEmpty();
        var assembly = Assembly.Load(output.ToArray());
        return assembly.GetType("Sample")!.GetMethod("Check")!.CreateDelegate<Action>();
    }
}
