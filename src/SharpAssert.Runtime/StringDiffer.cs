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
        if (left == null && right == null) return string.Empty;
        if (left == null) return $"  Left:  null\n  Right: {FormatStringValue(right)}";
        if (right == null) return $"  Left:  {FormatStringValue(left)}\n  Right: null";
        
        var leftTruncated = TruncateString(left);
        var rightTruncated = TruncateString(right);
        
        var basic = $"  Left:  {FormatStringValue(leftTruncated)}\n  Right: {FormatStringValue(rightTruncated)}";
        
        if (IsMultiline(left) || IsMultiline(right))
            return basic + "\n" + GenerateMultilineDiff(leftTruncated, rightTruncated);
        
        return basic + "\n" + GenerateInlineDiff(leftTruncated, rightTruncated);
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
        // For single-line strings, use character-level diff
        var differ = new Differ();
        var diffResult = differ.CreateCharacterDiffs(left, right, ignoreWhitespace: false);
        
        var result = "  Diff: ";
        foreach (var piece in diffResult.DiffBlocks)
        {
            if (piece.DeleteCountA > 0)
            {
                var deleted = left.Substring(piece.DeleteStartA, piece.DeleteCountA);
                result += $"[-{deleted}]";
            }
            if (piece.InsertCountB > 0)
            {
                var inserted = right.Substring(piece.InsertStartB, piece.InsertCountB);
                result += $"[+{inserted}]";
            }
        }
        
        // Add unchanged parts
        var leftPos = 0;
        var rightPos = 0;
        var resultBuilder = new System.Text.StringBuilder("  Diff: ");
        
        foreach (var block in diffResult.DiffBlocks)
        {
            // Add unchanged text before this block
            if (block.DeleteStartA > leftPos)
            {
                resultBuilder.Append(left.Substring(leftPos, block.DeleteStartA - leftPos));
            }
            
            // Add the changes
            if (block.DeleteCountA > 0)
            {
                var deleted = left.Substring(block.DeleteStartA, block.DeleteCountA);
                resultBuilder.Append($"[-{deleted}]");
            }
            if (block.InsertCountB > 0)
            {
                var inserted = right.Substring(block.InsertStartB, block.InsertCountB);
                resultBuilder.Append($"[+{inserted}]");
            }
            
            leftPos = block.DeleteStartA + block.DeleteCountA;
            rightPos = block.InsertStartB + block.InsertCountB;
        }
        
        // Add any remaining unchanged text
        if (leftPos < left.Length)
        {
            resultBuilder.Append(left.Substring(leftPos));
        }
        
        return resultBuilder.ToString();
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
            // Show deleted lines
            for (var i = 0; i < block.DeleteCountA; i++)
            {
                var lineIndex = block.DeleteStartA + i;
                if (lineIndex < leftLines.Length)
                {
                    result.Add($"  - {leftLines[lineIndex]}");
                }
            }
            
            // Show inserted lines
            for (var i = 0; i < block.InsertCountB; i++)
            {
                var lineIndex = block.InsertStartB + i;
                if (lineIndex < rightLines.Length)
                {
                    result.Add($"  + {rightLines[lineIndex]}");
                }
            }
        }
        
        if (result.Count == 0)
            result.Add("  (No specific line differences found)");
        
        if (result.Count > MaxDiffLines)
        {
            result = result.Take(MaxDiffLines).ToList();
            result.Add("  ... (output truncated)");
        }
        
        return string.Join("\n", result);
    }
}