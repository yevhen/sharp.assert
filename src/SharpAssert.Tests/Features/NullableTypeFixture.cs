using SharpAssert.Features.BinaryComparison;
using SharpAssert.Features.Shared;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class NullableTypeFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_show_null_state_for_nullable_int()
        {
            int? nullableValue = null;
            var nonNullValue = 42;

            var expected = BinaryComparison(
                "nullableValue == nonNullValue",
                Equal,
                NullableComparison(
                    Operand(nullableValue, typeof(int?)), 
                    Operand(nonNullValue, typeof(int?)), // Lifted to nullable
                    null, 42,
                    true, false,
                    typeof(int?), typeof(int?)));

            AssertFails(() => Assert(nullableValue == nonNullValue), expected);
        }

        [Test]
        public void Should_show_value_state_for_nullable_int()
        {
            int? nullableValue = 42;
            var nonNullValue = 24;

            var expected = BinaryComparison(
                "nullableValue == nonNullValue",
                Equal,
                NullableComparison(
                    Operand(nullableValue, typeof(int?)), 
                    Operand(nonNullValue, typeof(int?)), // Lifted
                    42, 24,
                    false, false,
                    typeof(int?), typeof(int?)));

            AssertFails(() => Assert(nullableValue == nonNullValue), expected);
        }

        [Test]
        public void Should_show_null_state_for_nullable_bool()
        {
            bool? nullableBool = null;
            var regularBool = true;

            var expected = BinaryComparison(
                "nullableBool == regularBool",
                Equal,
                NullableComparison(
                    Operand(nullableBool, typeof(bool?)), 
                    Operand(regularBool, typeof(bool?)), // Lifted
                    null, true,
                    true, false,
                    typeof(bool?), typeof(bool?)));

            AssertFails(() => Assert(nullableBool == regularBool), expected);
        }

        [Test]
        public void Should_show_null_state_for_nullable_DateTime()
        {
            DateTime? nullableDate = null;
            var regularDate = new DateTime(2023, 1, 1);

            var expected = BinaryComparison(
                "nullableDate == regularDate",
                Equal,
                NullableComparison(
                    Operand(nullableDate, typeof(DateTime?)), 
                    Operand(regularDate, typeof(DateTime?)), // Lifted
                    null, regularDate,
                    true, false,
                    typeof(DateTime?), typeof(DateTime?)));

            AssertFails(() => Assert(nullableDate == regularDate), expected);
        }

        [Test]
        public void Should_pass_when_both_nullable_values_are_null()
        {
            int? nullable1 = null;
            int? nullable2 = null;
            AssertPasses(() => Assert(nullable1 == nullable2));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_nullable_int_null()
        {
            var result = NullableComparison(
                Operand(null, typeof(int?)), 
                Operand(42, typeof(int)),
                null, 42,
                true, false,
                typeof(int?), typeof(int));

            AssertRendersExactly(result,
                "Left:  null",
                "Right: 42");
        }

        [Test]
        public void Should_render_nullable_int_value()
        {
            var result = NullableComparison(
                Operand(42, typeof(int?)), 
                Operand(24, typeof(int)),
                42, 24,
                false, false,
                typeof(int?), typeof(int));

            AssertRendersExactly(result,
                "Left:  42",
                "Right: 24");
        }
    }

    static NullableComparisonResult NullableComparison(
        AssertionOperand leftOp, AssertionOperand rightOp,
        object? leftVal, object? rightVal,
        bool leftNull, bool rightNull,
        Type? leftType, Type? rightType) =>
        new(leftOp, rightOp, leftVal, rightVal, leftNull, rightNull, leftType, rightType);
}
