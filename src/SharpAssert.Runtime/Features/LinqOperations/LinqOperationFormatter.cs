using System.Collections;
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;
using static SharpAssert.Features.Shared.EvaluationUnavailableHelpers;

namespace SharpAssert.Features.LinqOperations;

static class LinqOperationFormatter
{
    const int CollectionPreviewLimit = 10;
    
    public static FormattedEvaluationResult BuildResult(MethodCallExpression methodCall, string expressionText, bool value, Func<Expression, object?> getValue)
    {
        var methodName = methodCall.Method.Name;
        var collection = getValue(methodCall.Object ?? methodCall.Arguments[0]);

        if (IsUnavailable(collection))
            return new FormattedEvaluationResult(expressionText, value, new[] { DescribeUnavailable(collection) });
        
        var lines = methodName switch
        {
            "Contains" => FormatContainsFailure(methodCall, collection, getValue),
            "Any" => FormatAnyFailure(methodCall, collection),
            "All" => FormatAllFailure(methodCall),
            _ => new[] { "Unsupported LINQ operation" }
        };

        return new FormattedEvaluationResult(expressionText, value, lines);
    }
    
    public static FormattedEvaluationResult BuildCapturedAllResult(string expressionText, object? firstFailure, string predicateText) =>
        new(expressionText, false, [$"All failed: first item {ValueFormatter.Format(firstFailure)} did not match {predicateText}"]);

    public static FormattedEvaluationResult BuildCapturedContainsResult(string expressionText, object? collection, object? item) =>
        new(expressionText, false, FormatContainsFailure(collection, item));

    static IReadOnlyList<string> FormatContainsFailure(MethodCallExpression methodCall, object? collection, Func<Expression, object?> getValue) =>
        FormatContainsFailure(collection, getValue(methodCall.Arguments.Last()));

    static IReadOnlyList<string> FormatContainsFailure(object? collection, object? item)
    {
        if (IsUnavailable(item))
            return new[] { DescribeUnavailable(item) };

        var collectionStr = FormatCollection(collection);
        var count = GetCount(collection);
        
        return new[]
        {
            $"Contains failed: searched for {FormatValue(item)} in {collectionStr}",
            $"Count: {count}"
        };
    }
    
    static IReadOnlyList<string> FormatAnyFailure(MethodCallExpression methodCall, object? collection)
    {
        if (IsUnavailable(collection))
            return new[] { DescribeUnavailable(collection) };

        var count = GetCount(collection);
        
        if (count == 0)
            return new[] { "Any failed: collection is empty" };
        
        var collectionStr = FormatCollection(collection);
        var predicateStr = GetPredicateString(methodCall);
            
        return new[]
        {
            $"Any failed: no items matched {predicateStr} in {collectionStr}"
        };
    }
    
    static IReadOnlyList<string> FormatAllFailure(MethodCallExpression methodCall) =>
        [$"All failed: {GetPredicateString(methodCall)} returned false"];

    static string ExtractPredicateString(Expression predicateExpr) =>
        predicateExpr is LambdaExpression lambda ? lambda.ToString() : predicateExpr.ToString();
    
    static string FormatCollection(object? collection)
    {
        if (IsUnavailable(collection))
            return DescribeUnavailable(collection);

        if (collection is not IEnumerable enumerable) 
            return FormatValue(collection);
            
        var items = new List<object?>();
        var count = 0;
        
        foreach (var item in enumerable)
        {
            if (count < CollectionPreviewLimit)
                items.Add(item);
            count++;
        }
        
        var preview = string.Join(", ", items.Select(FormatValue));
        
        if (count > CollectionPreviewLimit)
            preview += ", ...";
            
        return $"[{preview}]";
    }
    
    static int GetCount(object? collection) => collection switch
    {
        ICollection coll => coll.Count,
        IEnumerable enumerable => enumerable.Cast<object>().Count(),
        _ => 0
    };
    
    static string FormatValue(object? value) => ValueFormatter.Format(value);
    
    static string GetPredicateString(MethodCallExpression methodCall) =>
        methodCall.Arguments.Count > 1 ?
            ExtractPredicateString(methodCall.Arguments[1]) : "predicate";
}
