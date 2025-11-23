using System.Collections;
using System.Linq;
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
    
    public static SequenceEqualComparisonResult BuildResult(MethodCallExpression methodCall, string expressionText, bool value)
    {
        var firstSequence = GetValue(methodCall.Object ?? methodCall.Arguments[0]);
        var secondSequence = GetValue(methodCall.Arguments.Count > 1 ? methodCall.Arguments[1] : methodCall.Arguments[0]);
        
        var hasComparer = methodCall.Arguments.Count > 2 ||
                         (methodCall.Object == null && methodCall.Arguments.Count > 2);
        
        if (firstSequence is not IEnumerable firstEnum || secondSequence is not IEnumerable secondEnum)
        {
            return new SequenceEqualComparisonResult(
                new AssertionOperand(firstSequence, firstSequence?.GetType() ?? typeof(object)),
                new AssertionOperand(secondSequence, secondSequence?.GetType() ?? typeof(object)),
                hasComparer,
                new[]
                {
                    "SequenceEqual failed: one or both operands are not sequences"
                });
        }
        
        // Materialize sequences to avoid multiple enumeration
        var firstList = MaterializeSequence(firstEnum);
        var secondList = MaterializeSequence(secondEnum);

        var lines = firstList.Count != secondList.Count
            ? FormatLengthMismatch(firstList, secondList)
            : FormatUnifiedDiff(firstList, secondList, hasComparer);

        return new SequenceEqualComparisonResult(
            new AssertionOperand(firstSequence, firstSequence.GetType()),
            new AssertionOperand(secondSequence, secondSequence.GetType()),
            hasComparer,
            lines);
    }
    
    static List<object?> MaterializeSequence(IEnumerable sequence) => sequence.Cast<object?>().ToList();

    static IReadOnlyList<string> FormatLengthMismatch(List<object?> first, List<object?> second)
    {
        var firstPreview = FormatSequencePreview(first);
        var secondPreview = FormatSequencePreview(second);
        
        return new[]
        {
            "SequenceEqual failed: length mismatch",
            $"Expected length: {second.Count}",
            $"Actual length:   {first.Count}",
            $"First:  {firstPreview}",
            $"Second: {secondPreview}"
        };
    }
    
    static IReadOnlyList<string> FormatUnifiedDiff(List<object?> first, List<object?> second, bool hasComparer)
    {
        var firstStrings = first.Select(FormatValue).ToArray();
        var secondStrings = second.Select(FormatValue).ToArray();

        var diffLines = ComputeDiffLines(firstStrings, secondStrings);

        return BuildDiffLines(diffLines, hasComparer);
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

    static IReadOnlyList<string> BuildDiffLines(List<string> diffLines, bool hasComparer)
    {
        var lines = new List<string>
        {
            "SequenceEqual failed: sequences differ"
        };

        if (hasComparer)
            lines.Add("(using custom comparer)");

        lines.Add("Unified diff:");

        if (diffLines.Count > MaxDiffLines)
        {
            lines.AddRange(diffLines.Take(MaxDiffLines));
            lines.Add("... (diff truncated)");
        }
        else
        {
            lines.AddRange(diffLines);
        }

        return lines;
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
        DateTime dt => dt.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };
    
    static object? GetValue(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
    
}
