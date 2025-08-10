using FluentAssertions;

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
            #line 1 "TestFile.cs"
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    #line 8 "TestFile.cs"
            global::SharpAssert.SharpInternal.Assert(()=>x == 1,"x == 1","TestFile.cs",8)
            #line default
            ; 
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

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
            #line 1 "TestFile.cs"
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var items = new[] { 1, 2, 3 };
                    #line 8 "TestFile.cs"
            global::SharpAssert.SharpInternal.Assert(()=>items.Contains(2) && items.Length > 0,"items.Contains(2) && items.Length > 0","TestFile.cs",8)
            #line default
            ; 
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

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

        var expected = source; // No rewrites should happen for async, so output == input

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

        result.Should().Be(expected);
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
            #line 1 "TestFile.cs"
            using static Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    var y = 2;
                    #line 9 "TestFile.cs"
            global::SharpAssert.SharpInternal.Assert(()=>x == 1,"x == 1","TestFile.cs",9)
            #line default
            ;
                    #line 10 "TestFile.cs"
            global::SharpAssert.SharpInternal.Assert(()=>y > x,"y > x","TestFile.cs",10)
            #line default
            ; 
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Be(expected);
    }
}