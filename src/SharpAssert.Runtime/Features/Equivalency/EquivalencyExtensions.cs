// ABOUTME: Extension methods for object equivalency assertions.
// ABOUTME: Provides IsEquivalentTo() for deep object comparison with configuration.

namespace SharpAssert;

public static class EquivalencyExtensions
{
    public static IsEquivalentToExpectation<T> IsEquivalentTo<T>(this T actual, T expected)
    {
        return new IsEquivalentToExpectation<T>(actual, expected, new EquivalencyConfig<T>());
    }

    public static IsEquivalentToExpectation<T> IsEquivalentTo<T>(
        this T actual,
        T expected,
        Func<EquivalencyConfig<T>, EquivalencyConfig<T>> configure)
    {
        var config = configure(new EquivalencyConfig<T>());
        return new IsEquivalentToExpectation<T>(actual, expected, config);
    }
}
