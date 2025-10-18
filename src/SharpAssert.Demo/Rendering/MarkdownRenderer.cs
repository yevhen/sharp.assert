using System.Text;

namespace SharpAssert.Demo.Rendering;

sealed class MarkdownRenderer(string outputFile = "demo.md") : IDemoRenderer, ISaveableRenderer
{
    readonly StringBuilder content = new();
    int currentCategoryNumber = 0;

    public void RenderHeader(string title)
    {
        content.AppendLine($"# {title}");
        content.AppendLine();
        content.AppendLine("This document showcases all supported features of SharpAssert with detailed diagnostic output.");
        content.AppendLine();
        content.AppendLine("---");
        content.AppendLine();
    }

    public void RenderCategory(DemoCategory category)
    {
        currentCategoryNumber++;
        content.AppendLine($"## {currentCategoryNumber}. {category.Name}");
        content.AppendLine();
        content.AppendLine($"> {category.Description}");
        content.AppendLine();
    }

    public void RenderDemo(DemoDefinition demo, DemoResult result)
    {
        // Demo header
        content.AppendLine($"### {demo.Name}");
        content.AppendLine($"**Description:** {demo.Description}");
        content.AppendLine();

        // Code section
        var code = demo.ExtractCode();
        content.AppendLine("**Code:**");
        content.AppendLine("```csharp");
        content.AppendLine(code);
        content.AppendLine("```");
        content.AppendLine();

        // Output section
        content.AppendLine("**Output:**");
        content.AppendLine("```");
        content.AppendLine(result.Output);
        content.AppendLine("```");
        content.AppendLine();
        content.AppendLine("---");
        content.AppendLine();
    }

    public void RenderFooter(int categoryCount, int totalDemos)
    {
        content.AppendLine("## Summary");
        content.AppendLine();
        content.AppendLine($"**Total categories demonstrated:** {categoryCount}");
        content.AppendLine($"**Total demo cases:** {totalDemos}");
        content.AppendLine();
        content.AppendLine("All assertions intentionally failed to showcase SharpAssert's rich diagnostic output.");
        content.AppendLine();
        content.AppendLine("SharpAssert provides pytest-style assertions with detailed failure messages, helping you understand exactly why your tests fail.");
    }

    public void Complete()
    {
        // Content is already built in _content
    }

    public void Save()
    {
        File.WriteAllText(outputFile, content.ToString());
    }
}
