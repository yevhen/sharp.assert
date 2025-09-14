using System.Collections;
using System.Linq.Expressions;

namespace SharpAssert;

/// <summary>Binary comparison operations for rewriter use.</summary>
public enum BinaryOp { Eq, Ne, Lt, Le, Gt, Ge }

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
    
    /// <summary>
    /// Validates an async boolean expression and provides basic failure information.
    /// </summary>
    /// <param name="conditionAsync">Async function that returns boolean to validate</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static async Task AssertAsync(
        Func<Task<bool>> conditionAsync,
        string expr,
        string file,
        int line)
    {
        var result = await conditionAsync();
        if (!result)
        {
            var context = new AssertionContext(expr, file, line, null);
            var failureMessage = FormatAsyncFailure(context);
            throw new SharpAssertionException(failureMessage);
        }
    }
    
    /// <summary>
    /// Validates an async binary comparison and provides detailed failure information.
    /// </summary>
    /// <param name="leftAsync">Async function that returns the left operand</param>
    /// <param name="rightAsync">Async function that returns the right operand</param>
    /// <param name="op">Binary comparison operator</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static async Task AssertAsyncBinary(
        Func<Task<object?>> leftAsync,
        Func<Task<object?>> rightAsync,
        BinaryOp op,
        string expr,
        string file,
        int line)
    {
        // Evaluate operands in source order (left then right)
        var leftValue = await leftAsync();
        var rightValue = await rightAsync();
        
        // Perform comparison using same logic as sync binary expressions
        var comparisonResult = EvaluateBinaryComparison(op, leftValue, rightValue);
        
        if (!comparisonResult)
        {
            var context = new AssertionContext(expr, file, line, null);
            var failureMessage = FormatAsyncBinaryFailure(leftValue, rightValue, context);
            throw new SharpAssertionException(failureMessage);
        }
    }
    
    static bool EvaluateBinaryComparison(BinaryOp op, object? leftValue, object? rightValue) => op switch
    {
        BinaryOp.Eq => Equals(leftValue, rightValue),
        BinaryOp.Ne => !Equals(leftValue, rightValue),
        BinaryOp.Lt => Comparer.Default.Compare(leftValue, rightValue) < 0,
        BinaryOp.Le => Comparer.Default.Compare(leftValue, rightValue) <= 0,
        BinaryOp.Gt => Comparer.Default.Compare(leftValue, rightValue) > 0,
        BinaryOp.Ge => Comparer.Default.Compare(leftValue, rightValue) >= 0,
        _ => false
    };
    
    static string FormatAsyncBinaryFailure(object? leftValue, object? rightValue, AssertionContext context)
    {
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        
        var baseMessage = context.Message is not null
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";
        
        var formatter = GetComparisonFormatter(leftValue, rightValue);
        return baseMessage + formatter.FormatComparison(leftValue, rightValue);
    }
    
    static IComparisonFormatter GetComparisonFormatter(object? leftValue, object? rightValue)
    {
        foreach (var formatter in ComparisonFormatters)
        {
            if (formatter.CanFormat(leftValue, rightValue))
                return formatter;
        }
        
        return DefaultFormatter;
    }
    
    static readonly IComparisonFormatter DefaultFormatter = new DefaultComparisonFormatter();

    static readonly IComparisonFormatter[] ComparisonFormatters =
    [
        new StringComparisonFormatter(),
        new CollectionComparisonFormatter(),
        new ObjectComparisonFormatter(),
    ];
    
    static string FormatAsyncFailure(AssertionContext context)
    {
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        return context.Message is not null
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n  Result: False"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n  Result: False";
    }
    
    static bool HasUnsupportedFeatures(Expression<Func<bool>> condition)
    {
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(condition);
        return detector.HasUnsupported;
    }
}