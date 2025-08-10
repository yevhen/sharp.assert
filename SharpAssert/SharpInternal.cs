using System.Collections;
using System.Linq.Expressions;

namespace SharpAssert;

/// <summary>Advanced assertion methods with detailed error reporting.</summary>
public static class SharpInternal
{
    /// <summary>
    /// Validates a boolean expression and provides detailed failure information.
    /// </summary>
    /// <param name="condition">Boolean expression to validate</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    /// <param name="message">Optional custom error message</param>
    /// <param name="usePowerAssert">Force PowerAssert for all assertions</param>
    /// <param name="usePowerAssertForUnsupported">Use PowerAssert for unsupported features</param>
    public static void Assert(
        Expression<Func<bool>> condition,
        string expr,
        string file,
        int line,
        string? message = null,
        bool usePowerAssert = false,
        bool usePowerAssertForUnsupported = true)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (usePowerAssert || usePowerAssertForUnsupported && HasUnsupportedFeatures(condition))
        {
            UsePowerAssert(condition, message);
            return;
        }

        var analyzer = new ExpressionAnalyzer();
        var context = new AssertionContext(expr, file, line, message);
        var failureMessage = analyzer.AnalyzeFailure(condition, context);
        
        if (string.IsNullOrEmpty(failureMessage))
            return;
            
        throw new SharpAssertionException(failureMessage);
    }
    
    static void UsePowerAssert(Expression<Func<bool>> condition, string? message)
    {
        try
        {
            PowerAssert.PAssert.IsTrue(condition);
        }
        catch (Exception ex)
        {
            var failureMessage = message is not null 
                ? $"{message}\n{ex.Message}"
                : ex.Message;

            var finalMessage = failureMessage.Replace("IsTrue", "Assert");

            throw new SharpAssertionException(finalMessage);
        }
    }
    
    static bool HasUnsupportedFeatures(Expression<Func<bool>> condition)
    {
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(condition);
        return detector.HasUnsupported;
    }
}

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