using FluentAssertions;
using SharpAssert.Core;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class AssertionResultSummaryFixture
{
    [TestCase(true, "Passed")]
    [TestCase(false, "Failed")]
    public void Should_summarize_assertion_results(bool passed, string expected)
    {
        var result = new AssertionEvaluationResult(new AssertionContext("check", "Test.cs", 1, null, new ExprNode("check")),
            new ValueEvaluationResult("check", passed, typeof(bool)));

        result.ToString().Should().Be(expected);
    }

    [Test]
    public void Should_keep_structured_results_and_primary_diagnostics()
    {
        var left = 1;
        var right = 2;
        Action assertion = () => Assert(left == right && right == 3);

        var error = assertion.Should().Throw<SharpAssertionException>().Which;

        error.Message.Should().Contain("left == right").And.Contain("Left:  1").And.Contain("Right: 2");
        error.Result!.Result.Should().BeOfType<LogicalEvaluationResult>();
        error.Result.ToString().Should().Be("Failed");
    }
}
