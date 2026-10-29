using FluentAssertions;
using static SharpAssert.Sharp;
using Number = System.Int32;

namespace SharpAssert;

[TestFixture]
public class EnumComparisonFixture
{
    [Test]
    public void Should_show_enum_names_in_comparisons()
    {
        var actual = Status.Running;
        Action assertion = () => Assert(actual == Status.Completed);

        ShouldRender(assertion, "actual == Status.Completed", "Running", "Completed");
    }

    [Test]
    public void Should_show_enum_names_in_compound_conditions()
    {
        var progress = new Progress();
        Func<bool> notReached = () => throw new InvalidOperationException("Not reached");
        Action assertion = () => Assert(progress.Status == Status.Completed && notReached());

        ShouldRender(assertion, "progress.Status == Status.Completed", "Running", "Completed");
        progress.Reads.Should().Be(1);
    }

    [Test]
    public void Should_preserve_enum_names_on_the_right()
    {
        var actual = Status.Running;
        Action assertion = () => Assert(Status.Completed == actual);

        ShouldRender(assertion, "Status.Completed == actual", "Completed", "Running");
    }

    [Test]
    public void Should_keep_inequality_results()
    {
        var actual = Status.Completed;
        Action assertion = () => Assert(actual != Status.Completed);

        ShouldRender(assertion, "actual != Status.Completed", "Completed", "Completed");
    }

    [Test]
    public void Should_pass_equal_enum_values()
    {
        var actual = Status.Completed;

        Assert(actual == Status.Completed);
    }

    [Test]
    public void Should_show_nullable_enum_names()
    {
        Status? actual = Status.Running;
        Status? expected = Status.Completed;
        Action assertion = () => Assert(actual == expected);

        ShouldRender(assertion, "actual == expected", "Running", "Completed");
    }

    [Test]
    public void Should_keep_null_nullable_enums()
    {
        Status? actual = null;
        Status? expected = Status.Completed;
        Action assertion = () => Assert(actual == expected);

        ShouldRender(assertion, "actual == expected", "null", "Completed");
    }

    [Test]
    public void Should_show_combined_flags()
    {
        var actual = Access.Read | Access.Write;
        Action assertion = () => Assert(actual == Access.Read);

        ShouldRender(assertion, "actual == Access.Read", "Read, Write", "Read");
    }

    [Test]
    public void Should_preserve_unsigned_enum_values()
    {
        var actual = Wide.High;
        Action assertion = () => Assert(actual == Wide.Low);

        ShouldRender(assertion, "actual == Wide.Low", "High", "Low");
    }

    [Test]
    public void Should_keep_unnamed_enum_values_numeric()
    {
        var actual = (Status)42;
        Action assertion = () => Assert(actual == Status.Completed);

        ShouldRender(assertion, "actual == Status.Completed", "42", "Completed");
    }

    [Test]
    public void Should_show_explicit_enum_cast_names()
    {
        var actual = 1;
        Action assertion = () => Assert((Status)actual == Status.Completed);

        ShouldRender(assertion, "(Status)actual == Status.Completed", "Running", "Completed");
    }

    [Test]
    public void Should_keep_nullable_numeric_casts()
    {
        Status? actual = Status.Running;
        Action assertion = () => Assert((int?)actual == 2 && actual == Status.Completed);

        ShouldRender(assertion, "(int?)actual == 2", "1", "2");
    }

    [Test]
    public void Should_not_repeat_failed_enum_reads()
    {
        var calls = 0;
        Func<Status> read = () => { calls++; throw new InvalidOperationException("Read failed"); };
        Action assertion = () => Assert(read() == Status.Completed);

        assertion.Should().Throw<SharpAssertionException>();

        calls.Should().Be(1);
    }

    [Test]
    public void Should_keep_explicit_numeric_casts()
    {
        var progress = new Progress();
        Action assertion = () => Assert((int)progress.Status == 2 && progress.Status == Status.Completed);

        ShouldRender(assertion, "(int)progress.Status == 2", "1", "2");
        progress.Reads.Should().Be(1);
    }

    [Test]
    public void Should_keep_captured_numeric_casts()
    {
        var progress = new Progress();
        Action assertion = () => Assert((int)progress.Status == 2 && true);

        ShouldRender(assertion, "(int)progress.Status == 2", "1", "2");
        progress.Reads.Should().Be(1);
    }

    [Test]
    public void Should_keep_checked_numeric_casts()
    {
        var progress = new Progress();
        Action assertion = () => Assert(checked((int)progress.Status) == 2 && progress.Status == Status.Completed);

        ShouldRender(assertion, "checked((int)progress.Status) == 2", "1", "2");
        progress.Reads.Should().Be(1);
    }

    [Test]
    public void Should_keep_numeric_cast_aliases()
    {
        var progress = new Progress();
        Action assertion = () => Assert((Number)progress.Status == 2 && progress.Status == Status.Completed);

        ShouldRender(assertion, "(Number)progress.Status == 2", "1", "2");
        progress.Reads.Should().Be(1);
    }

    static void ShouldRender(Action assertion, string expression, string left, string right)
    {
        var error = assertion.Should().Throw<SharpAssertionException>().Which;
        var details = $"  {expression}\n    Left:  {left}\n    Right: {right}";

        error.Message.Should().Be(error.Result!.Context.FormatMessage() + details.Replace("\n", Environment.NewLine));
    }

    sealed class Progress
    {
        public int Reads { get; set; }
        public Status Status { get { Reads++; return Status.Running; } }
    }

    enum Status { Running = 1, Completed = 2 }

    [Flags]
    enum Access { Read = 1, Write = 2 }

    enum Wide : ulong { Low = 0, High = ulong.MaxValue }
}
