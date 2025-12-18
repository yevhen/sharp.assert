using FluentAssertions;
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert;

public abstract class TestBase
{
    internal static void AssertFails(Action action, EvaluationResult expected)
    {
        var exception = action.Should().Throw<SharpAssertionException>().Which;
        exception.Result.Should().NotBeNull();
        exception.Result!.Result.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    internal static void AssertFails(Action action, AssertionEvaluationResult expected)
    {
        var exception = action.Should().Throw<SharpAssertionException>().Which;
        exception.Result.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    internal static void AssertPasses(Action action)
    {
        action.Should().NotThrow();
    }

    internal static async Task AssertFailsAsync(Func<Task> action, EvaluationResult expected)
    {
        var exception = (await action.Should().ThrowAsync<SharpAssertionException>()).Which;
        exception.Result.Should().NotBeNull();
        exception.Result!.Result.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    internal static async Task AssertFailsAsync(Func<Task> action, AssertionEvaluationResult expected)
    {
        var exception = (await action.Should().ThrowAsync<SharpAssertionException>()).Which;
        exception.Result.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }

    internal static void AssertRendersExactly(IReadOnlyList<RenderedLine> lines, params string[] expected)
    {
        var actual = string.Join("\n", lines.Select(l => l.Text));
        var expectedString = string.Join("\n", expected);
        actual.Should().Be(expectedString);
    }

    internal static void AssertRendersExactly<T>(T renderable, params string[] expected) where T : class
    {
        var lines = (IReadOnlyList<RenderedLine>)(renderable as dynamic).Render();
        AssertRendersExactly(lines, expected);
    }

    internal static void AssertRendersMessage(Action action, params string[] expected)
    {
        var exception = action.Should().Throw<SharpAssertionException>().Which;
        var lines = exception.Result!.Format().Split('\n');
        lines.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    internal static void AssertRendersMessageContains(Action action, params string[] expected)
    {
        var exception = action.Should().Throw<SharpAssertionException>().Which;
        var lines = exception.Result!.Format().Split('\n');
        lines.Should().ContainInConsecutiveOrder(expected);
    }

    internal static string Rendered(IReadOnlyList<RenderedLine> lines) =>
        string.Join("\n", lines.Select(l => l.Text));

    internal static string Rendered<T>(T renderable) where T : class
    {
        var lines = (IReadOnlyList<RenderedLine>)(renderable as dynamic).Render();
        return Rendered(lines);
    }

    internal static AssertionOperand Operand(object? value, Type? type = null) =>
        new(value, type ?? value?.GetType() ?? typeof(object));

    internal static BinaryComparisonEvaluationResult BinaryComparison(
        string expr,
        ExpressionType op,
        ComparisonResult comparison,
        bool value = false) =>
        new(expr, op, comparison, value);

    internal static ValueEvaluationResult Value(string expr, object? value, Type? type = null) =>
        new(expr, value, type ?? value?.GetType() ?? typeof(object));

    protected static readonly ExpressionType Equal = ExpressionType.Equal;
    protected static readonly ExpressionType NotEqual = ExpressionType.NotEqual;

    internal static ExpectationContext TestContext(string expression) =>
        new(expression, "test.cs", 1, null, new ExprNode(expression));
}
