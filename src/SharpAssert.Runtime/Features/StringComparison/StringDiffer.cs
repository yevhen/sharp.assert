using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SharpAssert.Features.StringComparison;

static class StringDiffer
{
    const int MaxStringLength = 1000;
    const int MaxDiffLines = 50;
    
    public static StringDiff FormatDiff(string? left, string? right)
    {
        if (left == null || right == null)
            return new InlineStringDiff(FormatNullSegments(left, right));

        var leftTruncated = TruncateString(left);
        var rightTruncated = TruncateString(right);

        if (IsMultiline(left) || IsMultiline(right))
            return new MultilineStringDiff(FormatMultilineComparisonLines(leftTruncated, rightTruncated));

        return new InlineStringDiff(GenerateInlineDiffSegments(leftTruncated, rightTruncated));
    }

    static IReadOnlyList<DiffSegment> FormatNullSegments(string? left, string? right)
    {
        if (left == null && right == null) return Array.Empty<DiffSegment>();
        if (left == null)
            return new[]
            {
                new DiffSegment(StringDiffOperation.Deleted, "null"),
                new DiffSegment(StringDiffOperation.Inserted, FormatStringValue(right))
            };
        return new[]
        {
            new DiffSegment(StringDiffOperation.Deleted, FormatStringValue(left)),
            new DiffSegment(StringDiffOperation.Inserted, "null")
        };
    }

    static IReadOnlyList<TextDiffLine> FormatMultilineComparisonLines(string left, string right)
    {
        return GenerateMultilineDiffLines(left, right);
    }

    static string FormatStringValue(string? value)
    {
        if (value == null) return "null";
        return $"\"{value}\"";
    }
    
    static string TruncateString(string input)
    {
        if (input.Length <= MaxStringLength) return input;
        return input.Substring(0, MaxStringLength) + "...";
    }
    
    static bool IsMultiline(string input) => input.Contains('\n');
    
    static IReadOnlyList<DiffSegment> GenerateInlineDiffSegments(string left, string right)
    {
        var differ = new Differ();
        var diffResult = differ.CreateCharacterDiffs(left, right, ignoreWhitespace: false);

        var leftPos = 0;
        var segments = new List<DiffSegment>();

        foreach (var block in diffResult.DiffBlocks)
        {
            AppendUnchangedText(segments, left, leftPos, block.DeleteStartA);
            AppendDeletedText(segments, left, block.DeleteStartA, block.DeleteCountA);
            AppendInsertedText(segments, right, block.InsertStartB, block.InsertCountB);

            leftPos = block.DeleteStartA + block.DeleteCountA;
        }

        AppendRemainingText(segments, left, leftPos);
        return segments;
    }

    static void AppendUnchangedText(List<DiffSegment> segments, string source, int start, int end)
    {
        if (end > start)
            segments.Add(new DiffSegment(StringDiffOperation.Unchanged, source.Substring(start, end - start)));
    }

    static void AppendDeletedText(List<DiffSegment> segments, string source, int start, int count)
    {
        if (count <= 0) return;

        var deleted = source.Substring(start, count);
        segments.Add(new DiffSegment(StringDiffOperation.Deleted, deleted));
    }

    static void AppendInsertedText(List<DiffSegment> segments, string source, int start, int count)
    {
        if (count <= 0) return;

        var inserted = source.Substring(start, count);
        segments.Add(new DiffSegment(StringDiffOperation.Inserted, inserted));
    }

    static void AppendRemainingText(List<DiffSegment> segments, string source, int position)
    {
        if (position < source.Length)
            segments.Add(new DiffSegment(StringDiffOperation.Unchanged, source.Substring(position)));
    }
    
    static IReadOnlyList<TextDiffLine> GenerateMultilineDiffLines(string left, string right)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(left, right, ignoreWhitespace: false);

        var result = new List<TextDiffLine>();

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Unchanged:
                    result.Add(new TextDiffLine(ChangeType.Unchanged, line.Text));
                    break;
                case ChangeType.Deleted:
                    result.Add(new TextDiffLine(ChangeType.Deleted, line.Text));
                    break;
                case ChangeType.Inserted:
                    result.Add(new TextDiffLine(ChangeType.Inserted, line.Text));
                    break;
                case ChangeType.Modified:
                    result.Add(new TextDiffLine(ChangeType.Modified, line.Text));
                    break;
            }
        }

        if (result.Count == 0)
            result.Add(new TextDiffLine(ChangeType.Unchanged, "(No specific line differences found)"));

        TruncateResultIfNeeded(result);
        return result;
    }

    static void TruncateResultIfNeeded(List<TextDiffLine> result)
    {
        if (result.Count <= MaxDiffLines) return;

        result.RemoveRange(MaxDiffLines, result.Count - MaxDiffLines);
        result.Add(new TextDiffLine(ChangeType.Unchanged, "... (output truncated)"));
    }
}
