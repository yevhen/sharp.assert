using FluentAssertions;
using SharpAssert.Core;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class CapturedAssertionsFixture
{
    [Test]
    public void Should_support_local_functions_in_logical_assertions()
    {
        var calls = 0;
        int Next() => ++calls;

        Action assertion = () => Assert(Next() == 1 && Next() == 3);

        assertion.Should().Throw<SharpAssertionException>();
        calls.Should().Be(2);
    }

    [Test]
    public void Should_use_project_global_usings_for_rewriting()
    {
        var source = "using static SharpAssert.Sharp; class Sample { void Check(Func<int> next) { Assert(next() == 1 && next() == 2); } }";

        var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs", "global using System;");

        rewritten.Should().Contain("SharpInternal.AssertCaptured");
    }

    [Test]
    public void Should_capture_static_method_arguments_once()
    {
        var calls = new List<int>();
        Func<int> first = () => { calls.Add(1); return 1; };
        Func<int> second = () => { calls.Add(2); return 2; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(IsEvenSum(first(), second()));
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Result.Should().BeEquivalentTo(
            new MethodCallEvaluationResult("IsEvenSum(first(), second())", false,
                [new ValueEvaluationResult("first()", 1, typeof(int)),
                 new ValueEvaluationResult("second()", 2, typeof(int))]));
        calls.Should().Equal(1, 2);
    }

    [Test]
    public void Should_evaluate_message_after_condition()
    {
        var calls = new List<string>();
        Func<int> left = () => { calls.Add("left"); return 1; };
        Func<int> right = () => { calls.Add("right"); return 2; };
        Func<string> message = () => { calls.Add("message"); return "Mismatch"; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(left() == 2 && right() == 1, message());
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Context.Message.Should().Be("Mismatch");
        calls.Should().Equal("left", "message");
    }

    [Test]
    public void Should_preserve_short_circuit_on_success()
    {
        var calls = 0;
        Func<bool> left = () => { calls++; return true; };
        Func<bool> right = () => throw new InvalidOperationException();

        Assert(left() || right());

        calls.Should().Be(1);
    }

    [Test]
    public void Should_reject_invalid_message_after_evaluating_condition()
    {
        var calls = 0;
        Func<bool> left = () => { calls++; return true; };
        Func<bool> right = () => { calls++; return true; };

        Action action = () => Assert(left() && right(), " ");

        action.Should().Throw<ArgumentException>();
        calls.Should().Be(2);
    }

    [Test]
    public void Should_keep_nested_failure_values_without_repeating_calls()
    {
        var calls = new List<int>();
        Func<int> first = () => { calls.Add(1); return 1; };
        Func<int> second = () => { calls.Add(2); return 2; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(first() == 1 && second() == 3);
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Format().Should().Contain("Right: 3");
        calls.Should().Equal(1, 2);
    }

    [Test]
    public void Should_rewrite_Enumerable_All_without_expression_tree()
    {
        var source = "using static SharpAssert.Sharp; class Sample { void Check(int[] items) { Assert(items.All(x => x > 0)); } }";

        var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs", "global using System; global using System.Linq;");

        rewritten.Should().Contain("SharpInternal.AssertAll");
    }

    [Test]
    public void Should_not_repeat_All_predicate_or_enumeration()
    {
        var yields = 0;
        var calls = 0;
        IEnumerable<int> Items()
        {
            yields++;
            yield return -1;
            yields++;
            yield return 0;
        }
        var items = Items();
        Func<int, bool> predicate = item => { calls++; return item > 0; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(items.All(predicate));
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Format().Should().Contain("first item -1");
        yields.Should().Be(1);
        calls.Should().Be(1);
    }

    [Test]
    public void Should_capture_static_Enumerable_All_failure_once()
    {
        var calls = 0;
        var items = new[] { -1, 0 };
        Func<int, bool> predicate = item => { calls++; return item > 0; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(Enumerable.All(items, predicate));
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Format().Should().Contain("first item -1");
        calls.Should().Be(1);
    }

    [Test]
    public void Should_preserve_Enumerable_All_null_source_error()
    {
        IEnumerable<int>? items = null;

        Action assertion = () => Assert(items!.All(item => item > 0));

        assertion.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Test]
    public void Should_capture_array_sequence_arguments_once()
    {
        var calls = 0;
        var actual = new[] { 1 };
        Func<int[]> expected = () => { calls++; return [2]; };

        SharpAssertionException? exception = null;
        try
        {
            Assert(actual.SequenceEqual(expected()));
        }
        catch (SharpAssertionException error)
        {
            exception = error;
        }

        exception.Should().NotBeNull();
        exception!.Result!.Format().Should().Contain("SequenceEqual failed");
        calls.Should().Be(1);
    }

    static bool IsEvenSum(int left, int right) => (left + right) % 2 == 0;
}
