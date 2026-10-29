using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class ShortCircuitAssertionFixture
{
    [Test]
    public void Should_skip_remaining_AND_conditions()
    {
        var calls = new List<int>();
        Func<bool> first = () => { calls.Add(1); return false; };
        Func<bool> second = () => { calls.Add(2); return true; };
        Func<bool> third = () => { calls.Add(3); return true; };
        Func<bool> fourth = () => { calls.Add(4); return true; };
        Action assertion = () => Assert(first() && second() && third() && fourth());

        assertion.Should().Throw<SharpAssertionException>();

        calls.Should().Equal(1);
    }

    [Test]
    public void Should_evaluate_reached_conditions_once_in_source_order()
    {
        var calls = new List<int>();
        Func<int> first = () => { calls.Add(1); return 1; };
        Func<int> second = () => { calls.Add(2); return 2; };
        Func<bool> third = () => throw new InvalidOperationException("Not reached");
        Action assertion = () => Assert(first() == 1 && second() == 3 && third());

        assertion.Should().Throw<SharpAssertionException>();

        calls.Should().Equal(1, 2);
    }

    [Test]
    public void Should_keep_null_guards()
    {
        string? text = null;
        Action assertion = () => Assert(text != null && text.Length == 3);

        var error = assertion.Should().Throw<SharpAssertionException>().Which;

        error.Result!.Result.Should().BeOfType<Core.LogicalEvaluationResult>()
            .Which.Right.Should().BeNull();
    }

    [Test]
    public void Should_keep_local_function_guards()
    {
        string? Read() => null;
        var text = Read();
        Action assertion = () => Assert(text != null && Read()!.Length == 3);

        assertion.Should().Throw<SharpAssertionException>();
    }

    [Test]
    public void Should_preserve_mixed_operator_groups()
    {
        var calls = new List<int>();
        Func<int, bool> check = index => { calls.Add(index); return index == 3; };
        Action assertion = () => Assert((check(1) && check(2)) || (check(3) && check(4)));

        assertion.Should().Throw<SharpAssertionException>();

        calls.Should().Equal(1, 3, 4);
    }

    [Test]
    public void Should_pass_without_evaluating_unused_OR_conditions()
    {
        var left = true;
        Func<bool> right = () => throw new InvalidOperationException("Not reached");

        Assert(left || right());
    }
}
