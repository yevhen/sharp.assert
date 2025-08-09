using FluentAssertions;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class DebugRewriterFixture
{
    [Test]
    public void Debug_rewriter_analysis()
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
        var result = rewriter.Rewrite(source, "TestFile.cs");
        
        // Just assert that the rewrite contains the expected parts
        result.Should().Contain("global::SharpInternal.Assert");
        result.Should().Contain("()=>x == 1"); // No space after =>
        result.Should().Contain("\"x == 1\"");
    }
}