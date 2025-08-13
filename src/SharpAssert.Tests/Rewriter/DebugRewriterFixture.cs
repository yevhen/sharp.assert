using FluentAssertions;

namespace SharpAssert.Rewriter.Tests;

[TestFixture]
public class DebugRewriterFixture
{
    [Test]
    public void Should_rewrite_sharp_assert_calls()
    {
        var source = """
            using static SharpAssert.Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    Assert(x == 1); 
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert");
        result.Should().Contain("()=>x == 1");
        result.Should().Contain("\"x == 1\"");
    }

    [Test]
    public void Should_not_rewrite_debug_assert_calls()
    {
        var source = """
            using System.Diagnostics;
            using static SharpAssert.Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var action = (Action)(() => {});
                    Debug.Assert(action != null, nameof(action) + " != null");
                    Assert(true); // This should still be rewritten
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Contain("Debug.Assert(action != null, nameof(action) + \" != null\");");
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert");
        result.Should().Contain("()=>true");
    }

    [Test]
    public void Should_not_rewrite_debug_assert_calls_via_using_static()
    {
        var source = """
            using static System.Diagnostics.Debug;
            
            class Test 
            { 
                void Method() 
                { 
                    var action = (Action)(() => {});
                    // This should NOT be rewritten (it's Debug.Assert via using static):
                    Assert(action != null, nameof(action) + " != null");
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Contain("Assert(action != null, nameof(action) + \" != null\");");
        result.Should().NotContain("global::SharpAssert.SharpInternal.Assert");
    }

    [Test]
    public void Should_not_rewrite_nunit_assert_calls()
    {
        var source = """
            using NUnit.Framework;
            using static SharpAssert.Sharp;
            
            class Test 
            { 
                void Method() 
                { 
                    var x = 1;
                    NUnit.Framework.Assert.AreEqual(1, x); // Fully qualified - should not be rewritten
                    Assert(x == 1); // Sharp.Assert - should be rewritten
                } 
            }
            """;

        var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");
        
        result.Should().Contain("NUnit.Framework.Assert.AreEqual(1, x);");
        result.Should().Contain("global::SharpAssert.SharpInternal.Assert");
        result.Should().Contain("()=>x == 1");
    }
}