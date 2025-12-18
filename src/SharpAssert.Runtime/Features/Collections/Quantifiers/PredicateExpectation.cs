// ABOUTME: Internal Expectation wrapper for bool predicates
// ABOUTME: Allows Each(x => x > 5) to work by wrapping bool predicates as Expectations

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

sealed class PredicateExpectation<T> : Expectation
{
    readonly T value;
    readonly Func<T, bool> predicate;
    readonly string predicateText;

    internal PredicateExpectation(T value, Func<T, bool> predicate, string predicateText)
    {
        this.value = value;
        this.predicate = predicate;
        this.predicateText = predicateText;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var result = predicate(value);
        return result
            ? ExpectationResults.Pass(context.Expression)
            : ExpectationResults.Fail(context.Expression,
                $"Value {ValueFormatter.Format(value)} did not satisfy: {predicateText}");
    }
}
