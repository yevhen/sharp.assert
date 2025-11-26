using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Evaluation;

class EvaluationRenderer
{
    public IReadOnlyList<RenderedLine> Render(EvaluationResult result) => result switch
    {
        AssertionEvaluationResult assertion => Render(assertion.Result),
        LogicalEvaluationResult logical => logical.Render(Render),
        UnaryEvaluationResult unary => unary.Render(Render),
        BinaryComparisonEvaluationResult binary => binary.Render(Render),
        ValueEvaluationResult value => value.Render(),
        FormattedEvaluationResult formatted => formatted.Render(),
        _ => Array.Empty<RenderedLine>()
    };

    IReadOnlyList<RenderedLine> Render(ComparisonResult comparison) => comparison switch
    {
        DefaultComparisonResult defaultResult => defaultResult.Render(),
        NullableComparisonResult nullable => nullable.Render(),
        StringComparisonResult stringResult => stringResult.Render(),
        CollectionComparisonResult collection => collection.Render(),
        ObjectComparisonResult @object => @object.Render(),
        SequenceEqualComparisonResult sequence => sequence.Render(),
        _ => Array.Empty<RenderedLine>()
    };
}
