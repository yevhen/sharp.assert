// ABOUTME: Tests for string wildcard pattern matching expectations.
// ABOUTME: Covers wildcard patterns (* and ?), case sensitivity, and diagnostic rendering.
using FluentAssertions;
using SharpAssert.Core;
using SharpAssert.Features.Strings;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class StringWildcardFixture : TestBase
{
    [TestFixture]
    class WildcardLogicTests
    {
        [Test]
        public void Should_pass_when_text_matches_exact_pattern()
        {
            var text = "hello world";

            AssertPasses(() => Assert(text.Matches("hello world")));
        }

        [Test]
        public void Should_pass_when_text_matches_asterisk_wildcard()
        {
            var text = "hello world";

            AssertPasses(() => Assert(text.Matches("hello *")));
        }

        [Test]
        public void Should_pass_when_text_matches_question_wildcard()
        {
            var text = "test";

            AssertPasses(() => Assert(text.Matches("t?st")));
        }

        [Test]
        public void Should_pass_case_insensitive_match()
        {
            var text = "HELLO WORLD";

            AssertPasses(() => Assert(text.MatchesIgnoringCase("hello world")));
        }
    }

    [TestFixture]
    class OccurrenceLogicTests
    {
        [Test]
        public void Should_pass_when_exact_occurrence_count_matches()
        {
            var text = "error at line 5, error at line 10";

            AssertPasses(() => Assert(text.Contains("error", Occur.Exactly(2))));
        }

        [Test]
        public void Should_fail_when_exact_occurrence_count_mismatches()
        {
            var text = "error at line 5, error at line 10, error at line 15";
            var action = () => Assert(text.Contains("error", Occur.Exactly(2)));

            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_at_least_constraint_satisfied()
        {
            var text = "error at line 5, error at line 10, error at line 15";

            AssertPasses(() => Assert(text.Contains("error", Occur.AtLeast(2))));
        }

        [Test]
        public void Should_fail_when_at_least_constraint_not_satisfied()
        {
            var text = "error at line 5";
            var action = () => Assert(text.Contains("error", Occur.AtLeast(2)));

            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_at_most_constraint_satisfied()
        {
            var text = "error at line 5";

            AssertPasses(() => Assert(text.Contains("error", Occur.AtMost(2))));
        }

        [Test]
        public void Should_fail_when_at_most_constraint_exceeded()
        {
            var text = "error at line 5, error at line 10, error at line 15";
            var action = () => Assert(text.Contains("error", Occur.AtMost(2)));

            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_handle_zero_occurrences()
        {
            var text = "success";
            var action = () => Assert(text.Contains("error", Occur.Exactly(1)));

            action.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class RegexOccurrenceLogicTests
    {
        [Test]
        public void Should_pass_when_regex_matches_exact_count()
        {
            var text = "test123 and test456 and test789";

            AssertPasses(() => Assert(text.MatchesRegex(@"test\d+", Occur.Exactly(3))));
        }

        [Test]
        public void Should_fail_when_regex_count_mismatches()
        {
            var text = "test123 and test456";
            var action = () => Assert(text.MatchesRegex(@"test\d+", Occur.Exactly(3)));

            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_regex_at_least_satisfied()
        {
            var text = "error: foo, error: bar, error: baz";

            AssertPasses(() => Assert(text.MatchesRegex(@"error:\s+\w+", Occur.AtLeast(2))));
        }

        [Test]
        public void Should_fail_when_regex_at_least_not_satisfied()
        {
            var text = "error: foo";
            var action = () => Assert(text.MatchesRegex(@"error:\s+\w+", Occur.AtLeast(2)));

            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_regex_at_most_satisfied()
        {
            var text = "warn: foo";

            AssertPasses(() => Assert(text.MatchesRegex(@"warn:\s+\w+", Occur.AtMost(2))));
        }

        [Test]
        public void Should_fail_when_regex_at_most_exceeded()
        {
            var text = "warn: foo, warn: bar, warn: baz";
            var action = () => Assert(text.MatchesRegex(@"warn:\s+\w+", Occur.AtMost(2)));

            action.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pattern_match_failure()
        {
            var text = "hello earth";
            var expectation = text.Matches("hello w*");
            var context = new ExpectationContext(
                "text.Matches(\"hello w*\")",
                "test.cs",
                1,
                null,
                new ExprNode("text.Matches(\"hello w*\")"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected text to match pattern \"hello w*\" but it did not.",
                "Actual: \"hello earth\"");
        }

        [Test]
        public void Should_render_case_insensitive_failure()
        {
            var text = "goodbye world";
            var expectation = text.MatchesIgnoringCase("hello *");
            var context = new ExpectationContext(
                "text.MatchesIgnoringCase(\"hello *\")",
                "test.cs",
                1,
                null,
                new ExprNode("text.MatchesIgnoringCase(\"hello *\")"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected text to match pattern \"hello *\" but it did not.",
                "Actual: \"goodbye world\"");
        }

        [Test]
        public void Should_render_occurrence_exact_failure()
        {
            var text = "error at line 5, error at line 10, error at line 15";
            var expectation = text.Contains("error", Occur.Exactly(2));
            var context = new ExpectationContext(
                "text.Contains(\"error\", Occur.Exactly(2))",
                "test.cs",
                1,
                null,
                new ExprNode("text.Contains(\"error\", Occur.Exactly(2))"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected substring \"error\" to appear exactly 2 time(s), but found 3.",
                "Actual: \"error at line 5, error at line 10, error at line 15\"");
        }

        [Test]
        public void Should_render_occurrence_at_least_failure()
        {
            var text = "error at line 5";
            var expectation = text.Contains("error", Occur.AtLeast(3));
            var context = new ExpectationContext(
                "text.Contains(\"error\", Occur.AtLeast(3))",
                "test.cs",
                1,
                null,
                new ExprNode("text.Contains(\"error\", Occur.AtLeast(3))"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected substring \"error\" to appear at least 3 time(s), but found 1.",
                "Actual: \"error at line 5\"");
        }

        [Test]
        public void Should_render_regex_occurrence_failure()
        {
            var text = "test123 and test456";
            var expectation = text.MatchesRegex(@"test\d+", Occur.Exactly(3));
            var context = new ExpectationContext(
                @"text.MatchesRegex(@""test\d+"", Occur.Exactly(3))",
                "test.cs",
                1,
                null,
                new ExprNode(@"text.MatchesRegex(@""test\d+"", Occur.Exactly(3))"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                @"Expected regex pattern ""test\d+"" to match exactly 3 time(s), but found 2 match(es).",
                "Actual: \"test123 and test456\"");
        }
    }
}
