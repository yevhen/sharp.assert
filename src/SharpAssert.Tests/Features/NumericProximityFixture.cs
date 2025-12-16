// ABOUTME: Tests for numeric proximity expectations (BeCloseTo, BeApproximately)
// ABOUTME: Validates that numeric values are within a specified tolerance of expected values

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Proximity;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class NumericProximityFixture : TestBase
{
    [TestFixture]
    class BeCloseToTests
    {
        [Test]
        public void Should_pass_when_within_tolerance()
        {
            var value = 10.5;
            var target = 10.0;
            var tolerance = 1.0;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_pass_when_exactly_equal()
        {
            var value = 10.0;
            var target = 10.0;
            var tolerance = 0.1;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_pass_when_at_tolerance_boundary()
        {
            var value = 11.0;
            var target = 10.0;
            var tolerance = 1.0;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_fail_when_outside_tolerance()
        {
            var value = 12.0;
            var target = 10.0;
            var tolerance = 1.0;

            var act = () => Assert(value.BeCloseTo(target, tolerance));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_work_with_negative_values()
        {
            var value = -5.2;
            var target = -5.0;
            var tolerance = 0.5;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_work_with_integers()
        {
            var value = 100;
            var target = 98;
            var tolerance = 5;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_work_with_floats()
        {
            var value = 3.14f;
            var target = 3.0f;
            var tolerance = 0.2f;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }

        [Test]
        public void Should_work_with_decimals()
        {
            var value = 100.50m;
            var target = 100.00m;
            var tolerance = 1.00m;

            AssertPasses(() => Assert(value.BeCloseTo(target, tolerance)));
        }
    }

    [TestFixture]
    class BeApproximatelyTests
    {
        [Test]
        public void Should_pass_when_within_tolerance()
        {
            var value = 3.14159;
            var target = 3.14;
            var tolerance = 0.01;

            AssertPasses(() => Assert(value.BeApproximately(target, tolerance)));
        }

        [Test]
        public void Should_fail_when_outside_tolerance()
        {
            var value = 3.5;
            var target = 3.0;
            var tolerance = 0.1;

            var act = () => Assert(value.BeApproximately(target, tolerance));
            act.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_show_actual_expected_and_tolerance_on_failure()
        {
            var context = new ExpectationContext("test", "", 0, null, new ExprNode("test"));
            var result = new NumericProximityExpectation<double>(15.0, 10.0, 2.0)
                .Evaluate(context);

            var rendered = result.Render();
            rendered.Should().Contain(line => line.Text.Contains("Actual"));
            rendered.Should().Contain(line => line.Text.Contains("Expected"));
            rendered.Should().Contain(line => line.Text.Contains("Tolerance"));
        }
    }
}
