namespace SharpAssert.Features;

[TestFixture]
public class DynamicAssertionFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_handle_dynamic_binary_failure()
        {
            var expected = BinaryComparison(
                "d == 5",
                Equal,
                Comparison(42, 5));

            AssertFails(() => SharpInternal.AssertDynamicBinary(
                () => 42,
                () => 5,
                BinaryOp.Eq,
                "d == 5",
                "File.cs",
                1), expected);
        }

        [Test]
        public void Should_handle_dynamic_simple_failure()
        {
            var expected = Value("d.Method()", false, typeof(bool));

            AssertFails(() => SharpInternal.AssertDynamic(
                () => false,
                "d.Method()",
                "File.cs",
                1), expected);
        }

        [Test]
        public void Should_pass_dynamic_binary()
        {
            AssertPasses(() => SharpInternal.AssertDynamicBinary(
                () => 42,
                () => 42,
                BinaryOp.Eq,
                "d == 42",
                "File.cs",
                1));
        }

        [Test]
        public void Should_pass_dynamic_simple()
        {
            AssertPasses(() => SharpInternal.AssertDynamic(
                () => true,
                "d.IsValid",
                "File.cs",
                1));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_dynamic_binary_failure()
        {
            var result = BinaryComparison(
                "d == 5",
                Equal,
                Comparison(42, 5));

            AssertRendersExactly(result,
                "d == 5",
                "Left:  42",
                "Right: 5");
        }

        [Test]
        public void Should_render_dynamic_simple_failure()
        {
            var result = Value("d.Method()", false, typeof(bool));

            AssertRendersExactly(result,
                "False");
        }
    }

    static BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));
}
