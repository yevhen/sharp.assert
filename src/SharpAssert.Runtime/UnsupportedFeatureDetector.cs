using System.Linq.Expressions;

namespace SharpAssert;

internal class UnsupportedFeatureDetector : ExpressionVisitor
{
    public bool HasUnsupported { get; private set; }
    
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // SequenceEqual is now supported as of Increment 9
        return base.VisitMethodCall(node);
    }
}