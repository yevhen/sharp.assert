namespace SharpAssert;

static class ComparisonFormatterService
{
    static readonly IComparisonFormatter DefaultFormatter = new DefaultComparisonFormatter();

    static readonly IComparisonFormatter[] ComparisonFormatters =
    [
        new NullableComparisonFormatter(),
        new StringComparisonFormatter(),
        new CollectionComparisonFormatter(),
        new ObjectComparisonFormatter(),
    ];

    public static IComparisonFormatter GetComparisonFormatter(AssertionOperand left, AssertionOperand right)
    {
        foreach (var formatter in ComparisonFormatters)
        {
            if (formatter.CanFormat(left, right))
                return formatter;
        }

        return DefaultFormatter;
    }
}