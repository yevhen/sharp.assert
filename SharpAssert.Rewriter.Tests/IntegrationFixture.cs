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
                        Assert(x == 2); // Should fail
                        return false; // Shouldn't reach here
                    } 
                    catch (SharpAssertionException ex)
                    {
                        return ex.Message.Contains("x == 2"); // Should contain expression
                    }
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var rewrittenSource = rewriter.Rewrite(source, "Test.cs");
        
        // Verify the rewrite happened
        rewrittenSource.Should().Contain("global::SharpInternal.Assert");
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
        var result = rewriter.Rewrite(source, "Test.cs");
        
        // Should remain unchanged
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
        var result = rewriter.Rewrite(source, "Test.cs");
        
        // Should rewrite all three assertions
        result.Should().Contain("global::SharpInternal.Assert(()=>x < y");
        result.Should().Contain("global::SharpInternal.Assert(()=>y > 0");
        result.Should().Contain("global::SharpInternal.Assert(()=>x + y == 3");
    }
}