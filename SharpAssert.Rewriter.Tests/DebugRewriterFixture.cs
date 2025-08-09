using FluentAssertions;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class DebugRewriterFixture
{
    [Test]
    public void DebugRewriterAnalysis()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    Assert(x == 1); 
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Contain("global::SharpInternal.Assert");
        result.Should().Contain("()=>x == 1");
        result.Should().Contain("\"x == 1\"");
    }
}