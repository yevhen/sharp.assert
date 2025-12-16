// ABOUTME: Tests for DateTime proximity expectations (BeCloseTo)
// ABOUTME: Validates that DateTime values are within a specified TimeSpan tolerance

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Proximity;
using static SharpAssert.Sharp;
using System.Globalization;

namespace SharpAssert.Features;

[TestFixture]
public class DateTimeProximityFixture : TestBase
{
    [TestFixture]
    class DateTimeBeCloseToTests
    {
        [Test]
        public void Should_pass_when_within_tolerance()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 0);
            var expected = new DateTime(2024, 1, 1, 12, 0, 30);
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }

        [Test]
        public void Should_pass_when_exactly_equal()
        {
            var dt = new DateTime(2024, 1, 1, 12, 0, 0);
            var tolerance = TimeSpan.FromSeconds(1);

            AssertPasses(() => Assert(dt.BeCloseTo(dt, tolerance)));
        }

        [Test]
        public void Should_pass_when_at_tolerance_boundary()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 0);
            var expected = new DateTime(2024, 1, 1, 12, 1, 0);
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }

        [Test]
        public void Should_fail_when_outside_tolerance()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 0);
            var expected = new DateTime(2024, 1, 1, 12, 5, 0);
            var tolerance = TimeSpan.FromMinutes(1);

            var act = () => Assert(actual.BeCloseTo(expected, tolerance));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_work_when_actual_is_before_expected()
        {
            var actual = new DateTime(2024, 1, 1, 11, 59, 30);
            var expected = new DateTime(2024, 1, 1, 12, 0, 0);
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }

        [Test]
        public void Should_work_when_actual_is_after_expected()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 30);
            var expected = new DateTime(2024, 1, 1, 12, 0, 0);
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }

        [Test]
        public void Should_work_with_millisecond_precision()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 0, 50);
            var expected = new DateTime(2024, 1, 1, 12, 0, 0, 0);
            var tolerance = TimeSpan.FromMilliseconds(100);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }
    }

    [TestFixture]
    class DateTimeOffsetBeCloseToTests
    {
        [Test]
        public void Should_pass_when_within_tolerance()
        {
            var actual = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var expected = new DateTimeOffset(2024, 1, 1, 12, 0, 30, TimeSpan.Zero);
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }

        [Test]
        public void Should_fail_when_outside_tolerance()
        {
            var actual = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var expected = new DateTimeOffset(2024, 1, 1, 12, 5, 0, TimeSpan.Zero);
            var tolerance = TimeSpan.FromMinutes(1);

            var act = () => Assert(actual.BeCloseTo(expected, tolerance));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_account_for_different_offsets()
        {
            var actual = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(0));
            var expected = new DateTimeOffset(2024, 1, 1, 13, 0, 30, TimeSpan.FromHours(1));
            var tolerance = TimeSpan.FromMinutes(1);

            AssertPasses(() => Assert(actual.BeCloseTo(expected, tolerance)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_show_actual_expected_and_tolerance_on_failure()
        {
            var actual = new DateTime(2024, 1, 1, 12, 0, 0);
            var expected = new DateTime(2024, 1, 1, 13, 0, 0);
            var tolerance = TimeSpan.FromMinutes(1);

            var context = new ExpectationContext("test", "", 0, null, new ExprNode("test"));
            var result = new DateTimeProximityExpectation(actual, expected, tolerance)
                .Evaluate(context);

            var rendered = result.Render();
            rendered.Should().Contain(line => line.Text.Contains("Actual"));
            rendered.Should().Contain(line => line.Text.Contains("Expected"));
            rendered.Should().Contain(line => line.Text.Contains("Tolerance"));
        }
    }
}
