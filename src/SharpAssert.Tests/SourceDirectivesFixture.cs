using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace SharpAssert;

[TestFixture]
public class SourceDirectivesFixture
{
    [Test]
    public void Should_preserve_nullable_disable_before_rewriting()
    {
        var source = """
            #nullable disable
            using static SharpAssert.Sharp;
            public class Sample
            {
                public string Name { get; set; }
                public string Value { get; set; } = null;
                public static void Check() { Assert(true); }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_preserve_leading_directives_and_comments()
    {
        var source = """
            // Source header
            #define CHECK
            #pragma warning disable CS8618
            using static SharpAssert.Sharp;
            public class Sample
            {
                public string Name { get; set; }
                public static void Check()
                {
            #if CHECK
                    Assert(true);
            #else
                    throw new System.Exception("Directive was lost");
            #endif
                }
            }
            """;

        var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

        rewritten.Should().Contain("// Source header").And.Contain("#define CHECK");
        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_map_compiler_diagnostics_to_original_lines()
    {
        var source = """
            #nullable enable

            using static SharpAssert.Sharp;
            public class Sample
            {
                public static void Check() { Assert(true); }
                public string Name { get; set; } = null;
            }
            """;

        var diagnostic = RewrittenSource.Compile(source).GetDiagnostics()
            .Single(each => each.Id == "CS8625");

        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line.Should().Be(6);
    }
}
