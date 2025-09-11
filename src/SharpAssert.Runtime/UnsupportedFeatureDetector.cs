using System.Linq.Expressions;

namespace SharpAssert;

internal class UnsupportedFeatureDetector : ExpressionVisitor
{
    public bool HasUnsupported { get; private set; }
    
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;
        if (methodName is "Contains" or "Any" or "All" or "SequenceEqual")
        {
            HasUnsupported = true;
        }
        return base.VisitMethodCall(node);
    }
}