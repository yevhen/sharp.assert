using SharpAssert.Core;
using SharpAssert.Features.Shared;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class EdgeCaseFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_handle_null_operands()
        {
            string? nullString = null;
            var value = "test";

            // Binary comparison (null == "test")
            var expected = BinaryComparison(
                "nullString == value",
                Equal,
                new Features.BinaryComparison.DefaultComparisonResult(
                    Operand(null, typeof(string)),
                    Operand("test", typeof(string))));

            AssertFails(() => Assert(nullString == value), expected);
        }

        [Test]
        public void Should_handle_simple_boolean_property_false()
        {
            var obj = new TestObject { IsValid = false };
            
            var expected = Value("obj.IsValid", false, typeof(bool));

            AssertFails(() => Assert(obj.IsValid), expected);
        }

        [Test]
        public void Should_handle_simple_boolean_method_call_false()
        {
            var obj = new TestObject { IsValid = false };
            
            // No arguments -> ValueEvaluationResult
            var expected = Value("obj.GetValidationResult()", false, typeof(bool));

            AssertFails(() => Assert(obj.GetValidationResult()), expected);
        }

        [Test]
        public void Should_handle_boolean_constant_false()
        {
            var expected = Value("false", false, typeof(bool));
            AssertFails(() => Assert(false), expected);
        }

        [Test]
        public void Should_handle_reference_equality_false()
        {
            var objA = new NonComparableClass { Name = "A" };
            var objB = new DifferentNonComparableClass { Value = 10 };

            var expected = MethodCall(
                "ReferenceEquals(objA, objB)",
                false,
                Value("objA", objA, typeof(NonComparableClass)),
                Value("objB", objB, typeof(DifferentNonComparableClass)));

            AssertFails(() => Assert(ReferenceEquals(objA, objB)), expected);
        }

        [Test]
        public void Should_pass_when_true()
        {
            AssertPasses(() => Assert(true));
            var obj = new TestObject { IsValid = true };
            AssertPasses(() => Assert(obj.IsValid));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_display_method_call_arguments()
        {
            var objA = new NonComparableClass { Name = "A" };
            var objB = new NonComparableClass { Name = "B" };

            var result = MethodCall(
                "ReferenceEquals(objA, objB)",
                false,
                Value("objA", objA, typeof(NonComparableClass)),
                Value("objB", objB, typeof(NonComparableClass)));

            // TestHelpers.NonComparableClass overrides ToString() => Name!
            // So "A" and "B"
            
            AssertRendersExactly(result,
                "ReferenceEquals(objA, objB)",
                "Argument[0]: A",
                "Argument[1]: B",
                "Result: False");
        }

        [Test]
        public void Should_display_simple_boolean_method_call_with_arguments()
        {
            var result = MethodCall(
                "text.StartsWith(prefix)",
                false,
                Value("\"Goodbye\"", "Goodbye", typeof(string)));

            AssertRendersExactly(result,
                "text.StartsWith(prefix)",
                "Argument[0]: \"Goodbye\"",
                "Result: False");
        }
    }

    static MethodCallEvaluationResult MethodCall(string expr, bool value, params EvaluationResult[] args) =>
        new(expr, value, args);

    static Features.BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));
}

