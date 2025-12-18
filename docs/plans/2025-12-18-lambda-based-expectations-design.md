# Lambda-Based Expectations Design

## Problem

Creating custom expectations requires significant ceremony:
- Separate class file per expectation
- Class declaration with constructor parameters
- Override of `Evaluate` method
- Manual handling of `ExpectationContext.Expression`

For simple expectations, this is boilerplate overhead.

## Solution

Add `Expectation.From()` factory that accepts a predicate and failure message factory:

```csharp
public abstract class Expectation : IExpectation
{
    public static Expectation From(Func<bool> predicate, Func<string[]> onFail)
        => new LambdaExpectation(predicate, onFail);

    sealed class LambdaExpectation(Func<bool> predicate, Func<string[]> onFail) : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context)
        {
            return predicate()
                ? ExpectationResults.Pass(context.Expression)
                : ExpectationResults.Fail(context.Expression, onFail());
        }
    }
}
```

## API Design

- `predicate`: `Func<bool>` - returns true if expectation passes
- `onFail`: `Func<string[]>` - lazy factory for diagnostic lines (only called on failure)
- Expression text injected automatically by framework

## Example Transformation

Before:
```csharp
// RegexOccurrenceExpectation.cs
public sealed class RegexOccurrenceExpectation(...) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context) { ... }
}
```

After:
```csharp
// In StringExtensions.cs
public static Expectation MatchesRegex(this string text, string pattern, OccurrenceConstraint count)
{
    return Expectation.From(
        () => CheckOccurrence(CountMatches(text, pattern), count),
        () => FormatFailure(...)
    );
}
```

## Scope

### Convert to lambda-based:
- StringOccurrenceExpectation
- RegexOccurrenceExpectation
- StringWildcardExpectation
- NumericProximityExpectation<T>
- DateTimeProximityExpectation
- CollectionOrderingExpectation<T>
- CollectionUniquenessExpectation<T>
- ContainsInOrderExpectation<T>
- ContainsInConsecutiveOrderExpectation<T>

### Keep as classes:
- SatisfiesExpectation<T> (bipartite matching algorithm)
- IsEquivalentToExpectation<T> (wraps Compare-Net-Objects)
- AndExpectation, OrExpectation, NotExpectation (composition infrastructure)

## Implementation Steps

1. Add `Expectation.From` factory to `Expectation.cs`
2. Refactor String expectations into `StringExtensions.cs`
3. Refactor Proximity expectations into extensions file
4. Refactor Collection expectations into extensions file
5. Delete obsolete class files
6. Run tests after each group
