using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SharpAssert;

static class StringDiffer
{
    const int MaxStringLength = 1000;
    const int MaxDiffLines = 50;
    
    public static string FormatDiff(string? left, string? right)
    {
        if (left == null || right == null)
            return FormatNullComparison(left, right);

        var leftTruncated = TruncateString(left);
        var rightTruncated = TruncateString(right);

        var basic = $"  Left:  {FormatStringValue(leftTruncated)}\n  Right: {FormatStringValue(rightTruncated)}";

        if (IsMultiline(left) || IsMultiline(right))
            return basic + "\n" + GenerateMultilineDiff(leftTruncated, rightTruncated);

        return basic + "\n" + GenerateInlineDiff(leftTruncated, rightTruncated);
    }

    static string FormatNullComparison(string? left, string? right)
    {
        if (left == null && right == null) return string.Empty;
        if (left == null) return $"  Left:  null\n  Right: {FormatStringValue(right)}";
        return $"  Left:  {FormatStringValue(left)}\n  Right: null";
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
    
    static string GenerateInlineDiff(string left, string right)
    {
        var differ = new Differ();
        var diffResult = differ.CreateCharacterDiffs(left, right, ignoreWhitespace: false);

        var leftPos = 0;
        var resultBuilder = new System.Text.StringBuilder("  Diff: ");

        foreach (var block in diffResult.DiffBlocks)
        {
            AppendUnchangedText(resultBuilder, left, leftPos, block.DeleteStartA);
            AppendDeletedText(resultBuilder, left, block.DeleteStartA, block.DeleteCountA);
            AppendInsertedText(resultBuilder, right, block.InsertStartB, block.InsertCountB);

            leftPos = block.DeleteStartA + block.DeleteCountA;
        }

        AppendRemainingText(resultBuilder, left, leftPos);
        return resultBuilder.ToString();
    }

    static void AppendUnchangedText(System.Text.StringBuilder builder, string source, int start, int end)
    {
        if (end > start)
            builder.Append(source.Substring(start, end - start));
    }

    static void AppendDeletedText(System.Text.StringBuilder builder, string source, int start, int count)
    {
        if (count <= 0) return;

        var deleted = source.Substring(start, count);
        builder.Append($"[-{deleted}]");
    }

    static void AppendInsertedText(System.Text.StringBuilder builder, string source, int start, int count)
    {
        if (count <= 0) return;

        var inserted = source.Substring(start, count);
        builder.Append($"[+{inserted}]");
    }

    static void AppendRemainingText(System.Text.StringBuilder builder, string source, int position)
    {
        if (position < source.Length)
            builder.Append(source.Substring(position));
    }
    
    static string GenerateMultilineDiff(string left, string right)
    {
        var differ = new Differ();
        var diffResult = differ.CreateLineDiffs(left, right, ignoreWhitespace: false);

        var result = new List<string>();
        var leftLines = left.Split('\n');
        var rightLines = right.Split('\n');

        foreach (var block in diffResult.DiffBlocks)
        {
            AppendDeletedLines(result, leftLines, block.DeleteStartA, block.DeleteCountA);
            AppendInsertedLines(result, rightLines, block.InsertStartB, block.InsertCountB);
        }

        if (result.Count == 0)
            result.Add("  (No specific line differences found)");

        TruncateResultIfNeeded(result);
        return string.Join("\n", result);
    }

    static void AppendDeletedLines(List<string> output, string[] lines, int start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var lineIndex = start + i;
            if (lineIndex < lines.Length)
                output.Add($"  - {lines[lineIndex]}");
        }
    }

    static void AppendInsertedLines(List<string> output, string[] lines, int start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var lineIndex = start + i;
            if (lineIndex < lines.Length)
                output.Add($"  + {lines[lineIndex]}");
        }
    }

    static void TruncateResultIfNeeded(List<string> result)
    {
        if (result.Count <= MaxDiffLines) return;

        result.RemoveRange(MaxDiffLines, result.Count - MaxDiffLines);
        result.Add("  ... (output truncated)");
    }
}