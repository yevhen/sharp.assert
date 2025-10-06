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
    const int ContextLinesBefore = 3;
    
    public static string FormatSequenceEqual(MethodCallExpression methodCall, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);
        
        var firstSequence = GetValue(methodCall.Object ?? methodCall.Arguments[0]);
        var secondSequence = GetValue(methodCall.Arguments.Count > 1 ? methodCall.Arguments[1] : methodCall.Arguments[0]);
        
        var hasComparer = methodCall.Arguments.Count > 2 ||
                         (methodCall.Object == null && methodCall.Arguments.Count > 2);
        
        if (firstSequence is not IEnumerable firstEnum || secondSequence is not IEnumerable secondEnum)
            return $"{baseMessage}  SequenceEqual failed: one or both operands are not sequences";
        
        // Materialize sequences to avoid multiple enumeration
        var firstList = MaterializeSequence(firstEnum);
        var secondList = MaterializeSequence(secondEnum);

        return firstList.Count != secondList.Count
            ? FormatLengthMismatch(baseMessage, firstList, secondList)
            : FormatUnifiedDiff(baseMessage, firstList, secondList, hasComparer);
    }
    
    static List<object?> MaterializeSequence(IEnumerable sequence) => sequence.Cast<object?>().ToList();

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

        var diffLines = ComputeDiffLines(firstStrings, secondStrings);

        return BuildDiffMessage(baseMessage, diffLines, hasComparer);
    }

    static List<string> ComputeDiffLines(string[] first, string[] second)
    {
        var differ = new Differ();
        var diffResult = differ.CreateLineDiffs(
            string.Join("\n", first),
            string.Join("\n", second),
            ignoreWhitespace: false);

        return GenerateUnifiedDiffLines(first, second, diffResult);
    }

    static string BuildDiffMessage(string baseMessage, List<string> diffLines, bool hasComparer)
    {
        var result = $"{baseMessage}  SequenceEqual failed: sequences differ\n";

        if (hasComparer)
            result += "  (using custom comparer)\n";

        result += "  Unified diff:\n";

        result += diffLines.Count > MaxDiffLines
            ? string.Join("\n", diffLines.Take(MaxDiffLines)) + "\n  ... (diff truncated)"
            : string.Join("\n", diffLines);

        return result;
    }
    
    static List<string> GenerateUnifiedDiffLines(string[] first, string[] second, DiffResult diffResult)
    {
        var lines = new List<string>();
        var lastShownIndex = 0;

        foreach (var block in diffResult.DiffBlocks)
        {
            var contextStart = CalculateContextStart(lastShownIndex, block.DeleteStartA);

            for (var i = contextStart; i < block.DeleteStartA; i++)
                lines.Add($"   [{i}] {first[i]}");

            AddDiffLines(lines, first, block.DeleteStartA, block.DeleteCountA, '-');
            AddDiffLines(lines, second, block.InsertStartB, block.InsertCountB, '+');

            lastShownIndex = block.DeleteStartA + block.DeleteCountA;
        }

        return lines;
    }

    static void AddDiffLines(List<string> lines, string[] sequence, int start, int count, char prefix)
    {
        for (var i = 0; i < count; i++)
        {
            var index = start + i;
            lines.Add($"  {prefix}[{index}] {sequence[index]}");
        }
    }

    static int CalculateContextStart(int lastShown, int blockStart) =>
        Math.Max(lastShown, blockStart - ContextLinesBefore);

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