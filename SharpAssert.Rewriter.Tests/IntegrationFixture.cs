using FluentAssertions;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class IntegrationFixture
{
    [Test]
    public void Should_rewrite_and_execute_simple_assertion()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                public static bool RunTest() 
                { 
                    var x = 1;
                    try 
                    {
                        Assert(x == 2);
                        return false;
                    } 
                    catch (SharpAssertionException ex)
                    {
                        return ex.Message.Contains("x == 2");
                    }
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var rewrittenSource = SharpAssertRewriter.Rewrite(source, "Test.cs");

        rewrittenSource.Should().Contain("global::SharpAssert.SharpInternal.Assert");
        rewrittenSource.Should().Contain("()=>x == 2");
        rewrittenSource.Should().NotContain("Assert(x == 2)");
    }

    [Test]
    public void Should_preserve_files_with_no_assertions()
    {
        var source = """
            class Test 
            { 
                public void Method() 
                { 
                    var x = 1;
                    Console.WriteLine(x); 
                } 
            }
            """;

        var rewriter = new SharpAssertRewriter();
        var result = SharpAssertRewriter.Rewrite(source, "Test.cs");

        result.Should().Be(source);
    }

    [Test]
    public void Should_handle_multiple_assertions_correctly()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                public void Method() 
                { 
                    var x = 1;
                    var y = 2;
                    Assert(x < y);
                    Assert(y > 0);
                    Assert(x + y == 3); 
                } 
            }
            """;

        var rewriter = new SharpAssertRewriter();
        var result = SharpAssertRewriter.Rewrite(source, "Test.cs");
        
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert(()=>x < y");
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert(()=>y > 0");
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert(()=>x + y == 3");
    }
}