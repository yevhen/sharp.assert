namespace SharpAssert.Demo.Rendering;

sealed class ConsoleRenderer : IDemoRenderer
{
    public void RenderHeader(string title)
    {
        WriteColoredText(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(title.ToUpper());
            Console.WriteLine("================================================================================");
        });
        Console.WriteLine();
    }

    public void RenderCategory(DemoCategory category)
    {
        WriteColoredText(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine($"{category.Number}. {category.Name}");
            Console.WriteLine("================================================================================");
        });
        Console.WriteLine(category.Description);
        Console.WriteLine();
    }

    public void RenderDemo(DemoDefinition demo, DemoResult result)
    {
        WriteColoredLine(ConsoleColor.Yellow, $"Demo: {demo.Name}");
        Console.WriteLine($"Description: {demo.Description}");
        Console.WriteLine(new string('-', 80));

        var code = demo.ExtractCode();
        WriteSectionHeader("Code:");
        WriteColoredLines(code, ConsoleColor.Gray, "  ");
        Console.WriteLine();

        WriteSectionHeader("Output:");
        WriteColoredLine(result.IsSuccess ? ConsoleColor.White : ConsoleColor.Red, result.Output);
        Console.WriteLine();
    }

    public void RenderFooter(int categoryCount, int totalDemos)
    {
        WriteColoredText(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("DEMO SUMMARY");
            Console.WriteLine("================================================================================");
        });
        Console.WriteLine($"Total categories demonstrated: {categoryCount}");
        Console.WriteLine($"Total demo cases: {totalDemos}");
        Console.WriteLine();
        Console.WriteLine("All assertions intentionally failed to showcase SharpAssert's rich diagnostic output.");
        Console.WriteLine();
        WriteColoredText(ConsoleColor.Green, () =>
        {
            Console.WriteLine("SharpAssert provides pytest-style assertions with detailed failure messages,");
            Console.WriteLine("helping you understand exactly why your tests fail.");
        });
    }

    public void Complete()
    {
    }

    void WriteSectionHeader(string header)
    {
        WriteColoredLine(ConsoleColor.DarkGray, header);
    }

    void WriteColoredLine(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    void WriteColoredLines(string text, ConsoleColor color, string indent = "")
    {
        foreach (var line in text.Split('\n'))
            WriteColoredLine(color, $"{indent}{line}");
    }

    void WriteColoredText(ConsoleColor color, Action writeAction)
    {
        Console.ForegroundColor = color;
        writeAction();
        Console.ResetColor();
    }
}
