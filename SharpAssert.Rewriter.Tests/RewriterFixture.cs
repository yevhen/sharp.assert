using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class RewriterFixture
{
    [Test]
    public void Should_rewrite_simple_assertion_to_lambda()
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
        
        var expected = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    global::SharpInternal.Assert(()=>x == 1,"x == 1",@"",8); 
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var result = rewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Be(expected);
    }
    
    [Test] 
    public void Should_preserve_complex_expressions()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var items = new[] { 1, 2, 3 };
                    Assert(items.Contains(2) && items.Length > 0); 
                } 
            }
            """;
        
        var expected = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var items = new[] { 1, 2, 3 };
                    global::SharpInternal.Assert(()=>items.Contains(2) && items.Length > 0,"items.Contains(2) && items.Length > 0",@"",8); 
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var result = rewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Be(expected);
    }
    
    [Test]
    public void Should_skip_rewrite_if_async_present()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                async Task Method() 
                { 
                    Assert(await GetBoolAsync()); 
                } 
                
                Task<bool> GetBoolAsync() => Task.FromResult(true);
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var result = rewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Be(source); // Should remain unchanged
    }
    
    [Test]
    public void Should_handle_multiple_assertions_in_file()
    {
        var source = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    var y = 2;
                    Assert(x == 1);
                    Assert(y > x); 
                } 
            }
            """;
        
        var expected = """
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    var y = 2;
                    global::SharpInternal.Assert(()=>x == 1,"x == 1",@"",9);
                    global::SharpInternal.Assert(()=>y > x,"y > x",@"",10); 
                } 
            }
            """;
        
        var rewriter = new SharpAssertRewriter();
        var result = rewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Be(expected);
    }
}