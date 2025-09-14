using System.Collections;
using System.Linq.Expressions;
using System.Text;

namespace SharpAssert;

static class LinqOperationFormatter
{
    const int CollectionPreviewLimit = 10;
    
    public static string FormatLinqOperation(MethodCallExpression methodCall, AssertionContext context)
    {
        var methodName = methodCall.Method.Name;
        var collection = GetValue(methodCall.Object ?? methodCall.Arguments[0]);
        
        return methodName switch
        {
            "Contains" => FormatContainsFailure(methodCall, collection, context),
            "Any" => FormatAnyFailure(methodCall, collection, context),
            "All" => FormatAllFailure(methodCall, collection, context),
            _ => AssertionFormatter.FormatAssertionFailure(context)
        };
    }
    
    static string FormatContainsFailure(MethodCallExpression methodCall, object? collection, AssertionContext context)
    {
        var item = GetValue(methodCall.Arguments.Last()); // Contains item
        var baseMessage = FormatBaseMessage(context);
        var collectionStr = FormatCollection(collection);
        var count = GetCount(collection);
        
        return $"{baseMessage}  Contains failed: searched for {FormatValue(item)} in {collectionStr} (Count: {count})";
    }
    
    static string FormatAnyFailure(MethodCallExpression methodCall, object? collection, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);
        var count = GetCount(collection);
        
        if (count == 0)
            return $"{baseMessage}  Any failed: collection is empty";
        
        var collectionStr = FormatCollection(collection);
        var predicateStr = GetPredicateString(methodCall);
            
        return $"{baseMessage}  Any failed: no items matched {predicateStr} in {collectionStr}";
    }
    
    static string FormatAllFailure(MethodCallExpression methodCall, object? collection, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);

        var predicateStr = GetPredicateString(methodCall);
        var predicateArg = GetPredicateArgument(methodCall);

        var failingItems = FindFailingItems(collection, predicateArg);
        var failingStr = failingItems.Any()
            ? FormatCollection(failingItems)
            : FormatCollection(collection);
        
        return $"{baseMessage}  All failed: items {failingStr} did not match {predicateStr}";
    }

    static Expression? GetPredicateArgument(MethodCallExpression methodCall)
    {
        return methodCall.Arguments.Count > 1 ? methodCall.Arguments[1] : null;
    }

    static string ExtractPredicateString(Expression predicateExpr) =>
        predicateExpr is LambdaExpression lambda ? lambda.ToString() : predicateExpr.ToString();

    static IEnumerable<object?> FindFailingItems(object? collection, Expression? predicateExpr)
    {
        if (collection is not IEnumerable enumerable || predicateExpr == null)
            return [];

        var predicate = TryCompilePredicate(predicateExpr);

        return predicate == null ? [] : FindNonMatchingItems(enumerable, predicate);
    }
    
    static Delegate? TryCompilePredicate(Expression predicateExpr)
    {
        return CompilePredicate(predicateExpr);
    }

    public static object?[] FindNonMatchingItems(IEnumerable enumerable, Delegate predicate) =>
        enumerable.Cast<object?>().Where(item => IsMatching(item, predicate)).ToArray();

    static bool IsMatching(object? item, Delegate predicate)
    {
        var matches = (bool)predicate.DynamicInvoke(item)!;
        return !matches;
    }
    
    static string FormatCollection(object? collection)
    {
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
    
    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString()!
    };
    
    static object? GetValue(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
    
    static string FormatBaseMessage(AssertionContext context)
    {
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        return context.Message is not null 
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";
    }
    
    static string GetPredicateString(MethodCallExpression methodCall) => 
        methodCall.Arguments.Count > 1 ? 
            ExtractPredicateString(methodCall.Arguments[1]) : "predicate";
    
    static Delegate CompilePredicate(Expression predicateExpr) => 
        predicateExpr is LambdaExpression lambda ? 
            lambda.Compile() : 
            Expression.Lambda(predicateExpr).Compile();
}