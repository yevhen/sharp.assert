using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class PropertyPatternsFixture
{
    [Test]
    public void Should_support_property_patterns_through_the_build_rewriter()
    {
        object value = new PatternItem(42);

        Assert(value is PatternItem { Id: 42 });
        Assert(value is not null);
    }

    [Test]
    public void Should_report_property_pattern_failure_through_the_build_rewriter()
    {
        object value = new PatternItem(1);

        Action assertion = () => Assert(value is PatternItem { Id: 42 });

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("Id: 42");
    }

    [TestCase("value is Expected { Id: 42 }")]
    [TestCase("value is Expected { Child: { Id: 42 } }")]
    [TestCase("value is Expected { Id: > 40 and < 50 }")]
    [TestCase("value is Expected { Id: 1 } or Expected { Id: 42 }")]
    [TestCase("value is not Expected { Id: 1 }")]
    [TestCase("value is Expected matched")]
    [TestCase("value is Expected patternValue")]
    [TestCase("(value is Expected { Id: 42 })")]
    [TestCase("value is Expected matched ? matched.Id == 42 : false")]
    public void Should_compile_and_run_native_patterns(string condition)
    {
        var source = $$"""
            using SharpAssert;
            using static SharpAssert.Sharp;
            public class Sample
            {
                public sealed class Expected
                {
                    public int Id => 42;
                    public Expected? Child { get; init; }
                }
                public static void Check()
                {
                    object value = new Expected { Child = new Expected() };
                    Assert({{condition}});
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [TestCase("new Expected()", "Actual type:")]
    [TestCase("new object()", "System.Object")]
    [TestCase("null", "Actual: null")]
    public void Should_report_pattern_and_input_type_without_reading_properties_again(string input, string detail)
    {
        var source = $$"""
            using System;
            using SharpAssert;
            using static SharpAssert.Sharp;
            public class Sample
            {
                static int calls;
                static int reads;
                public sealed class Expected
                {
                    public int Id { get { reads++; return 0; } }
                }
                static object? Input() { calls++; return {{input}}; }
                public static void Check()
                {
                    try
                    {
                        Assert(Input() is Expected { Id: 42 }, "Pattern check");
                    }
                    catch (SharpAssertionException)
                    {
                        if (calls != 1 || reads != {{(input == "new Expected()" ? 1 : 0)}})
                            throw new Exception("Input or property was read again");
                        throw;
                    }
                }
            }
            """;
        var assertion = RewrittenSource.LoadCheck(source);

        var error = assertion.Should().Throw<SharpAssertionException>().Which;
        error.Result!.Context.Message.Should().Be("Pattern check");
        error.Message.Should().Contain("Input() is Expected { Id: 42 }").And.Contain(detail).And.Contain("Sample.cs:");
    }

    [TestCase("Id: 1, Other: 2", 0)]
    [TestCase("Id: 1 } or Expected { Id: 2", 0)]
    public void Should_keep_native_pattern_short_circuit_and_getter_reuse(string properties, int otherReads)
    {
        var source = $$"""
            using System;
            using SharpAssert;
            using static SharpAssert.Sharp;
            public class Sample
            {
                static int reads;
                static int otherReads;
                public sealed class Expected
                {
                    public int Id { get { reads++; return 0; } }
                    public int Other { get { otherReads++; return 0; } }
                }
                public static void Check()
                {
                    object value = new Expected();
                    try
                    {
                        Assert(value is Expected { {{properties}} });
                    }
                    catch (SharpAssertionException)
                    {
                        if (reads != 1 || otherReads != {{otherReads}})
                            throw new Exception("Pattern evaluation changed");
                        throw;
                    }
                }
            }
            """;

        RewrittenSource.LoadCheck(source).Should().Throw<SharpAssertionException>();
    }

    [Test]
    public void Should_evaluate_both_independent_pattern_checks_once()
    {
        var source = """
            using System;
            using SharpAssert;
            using static SharpAssert.Sharp;
            public class Sample
            {
                static int calls;
                public sealed class Expected { public int Id => 0; }
                static object Input() { calls++; return new Expected(); }
                public static void Check()
                {
                    try
                    {
                        Assert(Input() is Expected { Id: 1 } && Input() is Expected { Id: 2 });
                    }
                    catch (SharpAssertionException)
                    {
                        if (calls != 2) throw new Exception("A check was skipped or repeated");
                        throw;
                    }
                }
            }
            """;

        RewrittenSource.LoadCheck(source).Should().Throw<SharpAssertionException>();
    }

    [Test]
    public void Should_keep_unconditional_pattern_bindings_in_the_original_scope()
    {
        var source = """
            using static SharpAssert.Sharp;
            public class Sample
            {
                public static void Check()
                {
                    object value = 42;
                    Assert(value is var captured);
                    Assert(captured != null);
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_not_hide_constants_in_pattern_lambdas()
    {
        var source = """
            using static SharpAssert.Sharp;
            public class Sample
            {
                public sealed class Expected { public int Id => 42; }
                public static void Check()
                {
                    const int patternValue = 42;
                    object value = new Expected();
                    Assert(value is Expected { Id: patternValue });
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_keep_pattern_bindings_inside_native_conditional_checks()
    {
        var source = """
            using static SharpAssert.Sharp;
            public class Sample
            {
                public static void Check()
                {
                    object value = 42;
                    Assert(value is int number ? number == 42 : false);
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    record PatternItem(int Id);
}
