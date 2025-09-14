using System.Collections;
using System.Linq.Expressions;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;

namespace SharpAssert;

static class SequenceEqualFormatter
{
    const int MaxSequencePreview = 20;
    const int MaxDiffLines = 50;
    
    public static string FormatSequenceEqual(MethodCallExpression methodCall, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);
        
        // Get the two sequences being compared
        var firstSequence = GetValue(methodCall.Object ?? methodCall.Arguments[0]);
        var secondSequence = GetValue(methodCall.Arguments.Count > 1 ? methodCall.Arguments[1] : methodCall.Arguments[0]);
        
        // Handle custom comparer if present
        var hasComparer = methodCall.Arguments.Count > 2 || 
                         (methodCall.Object == null && methodCall.Arguments.Count > 2);
        
        if (firstSequence is not IEnumerable firstEnum || secondSequence is not IEnumerable secondEnum)
        {
            return $"{baseMessage}  SequenceEqual failed: one or both operands are not sequences";
        }
        
        // Materialize sequences to avoid multiple enumeration
        var firstList = MaterializeSequence(firstEnum);
        var secondList = MaterializeSequence(secondEnum);
        
        // Check for length mismatch
        if (firstList.Count != secondList.Count)
        {
            return FormatLengthMismatch(baseMessage, firstList, secondList);
        }
        
        // Generate unified diff
        return FormatUnifiedDiff(baseMessage, firstList, secondList, hasComparer);
    }
    
    static List<object?> MaterializeSequence(IEnumerable sequence)
    {
        var result = new List<object?>();
        foreach (var item in sequence)
            result.Add(item);
        return result;
    }
    
    static string FormatLengthMismatch(string baseMessage, List<object?> first, List<object?> second)
    {
        var firstPreview = FormatSequencePreview(first);
        var secondPreview = FormatSequencePreview(second);
        
        return $"{baseMessage}  SequenceEqual failed: length mismatch\n" +
               $"  Expected length: {second.Count}\n" +
               $"  Actual length:   {first.Count}\n" +
               $"  First:  {firstPreview}\n" +
               $"  Second: {secondPreview}";
    }
    
    static string FormatUnifiedDiff(string baseMessage, List<object?> first, List<object?> second, bool hasComparer)
    {
        var firstStrings = first.Select(FormatValue).ToArray();
        var secondStrings = second.Select(FormatValue).ToArray();
        
        var differ = new Differ();
        var diffResult = differ.CreateLineDiffs(
            string.Join("\n", firstStrings), 
            string.Join("\n", secondStrings), 
            ignoreWhitespace: false);
        
        var diffLines = GenerateUnifiedDiffLines(firstStrings, secondStrings, diffResult);
        
        var result = $"{baseMessage}  SequenceEqual failed: sequences differ\n";
        
        if (hasComparer)
            result += "  (using custom comparer)\n";
            
        result += "  Unified diff:\n";
        
        if (diffLines.Count > MaxDiffLines)
        {
            result += string.Join("\n", diffLines.Take(MaxDiffLines));
            result += "\n  ... (diff truncated)";
        }
        else
        {
            result += string.Join("\n", diffLines);
        }
        
        return result;
    }
    
    static List<string> GenerateUnifiedDiffLines(string[] first, string[] second, DiffResult diffResult)
    {
        var lines = new List<string>();
        var firstIndex = 0;
        var secondIndex = 0;
        
        foreach (var block in diffResult.DiffBlocks)
        {
            // Show some context before changes
            var contextStart = Math.Max(0, block.DeleteStartA - 2);
            var contextEnd = Math.Min(first.Length, block.DeleteStartA);
            
            for (var i = contextStart; i < contextEnd && i < firstIndex; i++)
            {
                lines.Add($"   [{i}] {first[i]}");
            }
            
            // Show deleted items from first sequence
            for (var i = 0; i < block.DeleteCountA; i++)
            {
                var index = block.DeleteStartA + i;
                if (index < first.Length)
                {
                    lines.Add($"  -[{index}] {first[index]}");
                }
            }
            
            // Show inserted items from second sequence  
            for (var i = 0; i < block.InsertCountB; i++)
            {
                var index = block.InsertStartB + i;
                if (index < second.Length)
                {
                    lines.Add($"  +[{index}] {second[index]}");
                }
            }
            
            firstIndex = block.DeleteStartA + block.DeleteCountA;
            secondIndex = block.InsertStartB + block.InsertCountB;
        }
        
        return lines;
    }
    
    static string FormatSequencePreview(List<object?> sequence)
    {
        if (sequence.Count == 0)
            return "[]";
        
        var preview = sequence.Take(MaxSequencePreview).Select(FormatValue);
        var result = $"[{string.Join(", ", preview)}";
        
        if (sequence.Count > MaxSequencePreview)
            result += ", ...";
            
        result += "]";
        return result;
    }
    
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
}