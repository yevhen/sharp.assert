using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpAssert.Demo;

static class SourceCodeExtractor
{
    public static string ExtractMethodSource(Delegate method)
    {
        try
        {
            var methodInfo = method.Method;
            if (methodInfo.DeclaringType == null)
                return $"// Source code not available for {methodInfo.Name}";

            var declaringType = methodInfo.DeclaringType;

            var sourceFile = GetSourceFilePath(declaringType);
            if (sourceFile == null)
                return $"// Could not locate project root (searched from: {Assembly.GetExecutingAssembly().Location})";

            if (!File.Exists(sourceFile))
                return $"// Source file not found: {sourceFile}";

            var sourceCode = File.ReadAllText(sourceFile);
            var methodSource = ExtractMethod(sourceCode, methodInfo.Name);

            return methodSource ?? $"// Could not extract source for {methodInfo.Name}";
        }
        catch (Exception ex)
        {
            return $"// Error extracting source: {ex.Message}";
        }
    }

    static string? GetSourceFilePath(Type type)
    {
        var projectRoot = FindProjectRoot();
        if (projectRoot == null)
            return null;

        var demosPath = Path.Combine(projectRoot, "Demos");
        return FindTypeSourceFile(demosPath, type.Name);
    }

    static string? FindTypeSourceFile(string directory, string typeName)
    {
        var matchingFiles = Directory.GetFiles(directory, $"*{typeName}.cs", SearchOption.TopDirectoryOnly);
        return matchingFiles.Length > 0 ? matchingFiles[0] : null;
    }

    static string? FindProjectRoot()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directory = Path.GetDirectoryName(assemblyLocation);

        while (directory != null)
        {
            var demosPath = Path.Combine(directory, "Demos");
            var csprojPath = Path.Combine(directory, "SharpAssert.Demo.csproj");

            if (Directory.Exists(demosPath) && File.Exists(csprojPath))
                return directory;

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    static string? ExtractMethod(string sourceCode, string methodName)
    {
        var methodPattern = $@"public\s+static\s+(?:void|Task|async\s+Task)\s+{Regex.Escape(methodName)}\s*\(\s*\)";
        var match = Regex.Match(sourceCode, methodPattern);

        if (!match.Success)
            return null;

        var methodStart = match.Index;

        var openBraceIndex = sourceCode.IndexOf('{', methodStart);
        if (openBraceIndex == -1)
            return null;

        var closeBraceIndex = FindMatchingCloseBrace(sourceCode, openBraceIndex);
        if (closeBraceIndex == -1)
            return null;

        var methodBody = sourceCode.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1);
        return CleanMethodBody(methodBody);
    }

    static int FindMatchingCloseBrace(string code, int openBraceIndex)
    {
        var braceCount = 0;

        for (var i = openBraceIndex; i < code.Length; i++)
        {
            if (code[i] == '{')
                braceCount++;
            else if (code[i] == '}')
            {
                braceCount--;
                if (braceCount == 0)
                    return i;
            }
        }

        return -1;
    }

    static string CleanMethodBody(string methodBody)
    {
        var lines = methodBody.Split('\n');
        var result = new StringBuilder();

        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l));
        var indentCounts = nonEmptyLines.Select(l => l.TakeWhile(char.IsWhiteSpace).Count());
        var minIndent = indentCounts.DefaultIfEmpty(0).Min();

        var firstNonEmpty = true;

        foreach (var line in lines)
        {
            if (firstNonEmpty && string.IsNullOrWhiteSpace(line))
                continue;

            firstNonEmpty = false;

            result.AppendLine(
                string.IsNullOrWhiteSpace(line) ? "" : Dedent(line, minIndent));
        }

        return result.ToString().TrimEnd('\r', '\n');
    }

    static string Dedent(string line, int indentSize) =>
        line.Length > indentSize
            ? line.Substring(indentSize)
            : line.TrimStart();
}
