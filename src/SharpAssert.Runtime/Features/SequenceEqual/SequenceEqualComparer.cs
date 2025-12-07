using System.Collections;
using System.Linq.Expressions;
using DiffPlex;
using DiffPlex.Model;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.SequenceEqual;

static class SequenceEqualComparer
{
    const int MaxSequencePreview = 20;
    const int MaxDiffLines = 50;
    const int ContextLinesBefore = 3;
    
    public static SequenceEqualComparisonResult BuildResult(MethodCallExpression methodCall)
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
                null,
                null,
                false,
                "SequenceEqual failed: one or both operands are not sequences");
        }
        
        // Materialize sequences to avoid multiple enumeration
        var firstList = MaterializeSequence(firstEnum);
        var secondList = MaterializeSequence(secondEnum);

        if (firstList.Count != secondList.Count)
        {
            var lengthMismatch = BuildLengthMismatch(firstList, secondList);
            return new SequenceEqualComparisonResult(
                new AssertionOperand(firstSequence, firstSequence.GetType()),
                new AssertionOperand(secondSequence, secondSequence.GetType()),
                hasComparer,
                lengthMismatch,
                null,
                false);
        }

        var diffLines = BuildUnifiedDiff(firstList, secondList, out var truncated);

        return new SequenceEqualComparisonResult(
            new AssertionOperand(firstSequence, firstSequence.GetType()),
            new AssertionOperand(secondSequence, secondSequence.GetType()),
            hasComparer,
            null,
            diffLines,
            truncated);
    }
    
    static List<object?> MaterializeSequence(IEnumerable sequence) => sequence.Cast<object?>().ToList();

    static SequenceLengthMismatch BuildLengthMismatch(List<object?> first, List<object?> second)
    {
        return new SequenceLengthMismatch(second.Count, first.Count, BuildSequencePreview(first), BuildSequencePreview(second));
    }
    
    static IReadOnlyList<SequenceDiffLine> BuildUnifiedDiff(List<object?> first, List<object?> second, out bool truncated)
    {
        var firstStrings = first.Select(FormatValue).ToArray();
        var secondStrings = second.Select(FormatValue).ToArray();

        var diffLines = ComputeDiffLines(firstStrings, secondStrings);

        return BuildDiffLines(diffLines, first, second, out truncated);
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

    static IReadOnlyList<SequenceDiffLine> BuildDiffLines(List<string> diffLines, IReadOnlyList<object?> firstValues, IReadOnlyList<object?> secondValues, out bool truncated)
    {
        truncated = diffLines.Count > MaxDiffLines;
        var slice = truncated ? diffLines.Take(MaxDiffLines) : diffLines;
        return slice.Select(line => ToDiffLine(line, firstValues, secondValues)).ToList();
    }
    
    static List<string> GenerateUnifiedDiffLines(string[] first, string[] second, DiffResult diffResult)
    {
        var lines = new List<string>();
        var lastShownIndex = 0;

        foreach (var block in diffResult.DiffBlocks)
        {
            var contextStart = CalculateContextStart(lastShownIndex, block.DeleteStartA);

            for (var i = contextStart; i < block.DeleteStartA; i++)
                lines.Add($" {i} {first[i]}");

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
            lines.Add($"{prefix}{index} {sequence[index]}");
        }
    }

    static int CalculateContextStart(int lastShown, int blockStart) =>
        Math.Max(lastShown, blockStart - ContextLinesBefore);

    static IReadOnlyList<object?> BuildSequencePreview(List<object?> sequence)
    {
        if (sequence.Count == 0)
            return [];
        
        var preview = sequence.Take(MaxSequencePreview - 1).ToList();

        if (sequence.Count > MaxSequencePreview - 1)
            preview.Add("...");

        return preview;
    }
    
    static string FormatValue(object? value) => ValueFormatter.Format(value);

    static SequenceDiffLine ToDiffLine(string line, IReadOnlyList<object?> firstValues, IReadOnlyList<object?> secondValues)
    {
        if (line.StartsWith("-"))
        {
            var parts = line[1..].TrimStart();
            var spaceIndex = parts.IndexOf(' ');
            var index = int.Parse(parts[..spaceIndex]);
            var value = firstValues[index];
            return new SequenceDiffLine(SequenceDiffOperation.Removed, index, value);
        }

        if (line.StartsWith("+"))
        {
            var parts = line[1..].TrimStart();
            var spaceIndex = parts.IndexOf(' ');
            var index = int.Parse(parts[..spaceIndex]);
            var value = secondValues[index];
            return new SequenceDiffLine(SequenceDiffOperation.Added, index, value);
        }

        var trimmed = line.TrimStart();
        var idxSpace = trimmed.IndexOf(' ');
        var idx = int.Parse(trimmed[..idxSpace]);
        var val = firstValues[idx];
        return new SequenceDiffLine(SequenceDiffOperation.Context, idx, val);
    }

    static object? GetValue(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
}
