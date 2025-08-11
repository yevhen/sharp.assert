using System.Collections;
using System.Linq.Expressions;

namespace SharpAssert;

public class UnsupportedFeatureDetector : ExpressionVisitor
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
    
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // String comparisons (need DiffPlex)
        if (node.Left.Type == typeof(string) && node.Right.Type == typeof(string))
            HasUnsupported = true;
        
        // Collection comparisons
        if (IsCollection(node.Left.Type) || IsCollection(node.Right.Type))
            HasUnsupported = true;
        
        return base.VisitBinary(node);
    }
    
    static bool IsCollection(Type type)
    {
        return type != typeof(string) && 
               typeof(IEnumerable).IsAssignableFrom(type);
    }
}