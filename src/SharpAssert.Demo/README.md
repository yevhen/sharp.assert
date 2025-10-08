# SharpAssert.Demo

Automated demonstration and documentation generator for SharpAssert's assertion capabilities.

## Overview

SharpAssert.Demo is a living documentation system that showcases all SharpAssert features through executable demos. It automatically generates documentation in multiple formats from a single source of truth—the demo code itself.

## Quick Start

```bash
# View all demos in console
dotnet run

# Generate markdown documentation
dotnet run -- --format markdown

# View specific category
dotnet run -- --category "STRING COMPARISONS"

# Get help
dotnet run -- --help
```

## Architecture

SharpAssert.Demo uses a **renderer pattern** to separate demo execution from output formatting:

```
┌─────────────────────┐
│   Demo Definition   │  ← Single source of truth
│                     │
└──────────┬──────────┘
           │
           ├──────────────────┬──────────────────┐
           │                  │                  │
    ┌──────▼──────┐    ┌──────▼──────┐   ┌──────▼──────┐
    │  Execute    │    │  Extract    │   │  Render     │
    │  Demo       │    │  Source     │   │  Output     │
    └──────┬──────┘    └──────┬──────┘   └──────┬──────┘
           │                  │                  │
           └──────────────────┴──────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │   IDemoRenderer   │
                    └─────────┬─────────┘
                              │
                 ┏────────────┴────────────┓
                 ┃                         ┃
          ┌──────▼──────┐         ┌────────▼────────┐
          │   Console   │         │    Markdown     │
          │  Renderer   │         │    Renderer     │
          └─────────────┘         └─────────────────┘
```

### Core Components

#### Models (`DemoDefinition.cs`, `DemoCategory.cs`)
- **DemoDefinition**: Represents a single demo with metadata, execution logic, and source extraction
- **DemoCategory**: Groups related demos into logical sections
- **DemoResult**: Captures execution outcome (success with output or error)

#### Renderers (`Rendering/*.cs`)
- **IDemoRenderer**: Interface defining rendering contract
- **ConsoleRenderer**: Colored console output with source code display
- **MarkdownRenderer**: Generates GitHub-flavored markdown documentation
- **ISaveableRenderer**: Optional interface for file-based renderers

#### Orchestration (`DemoRunner.cs`)
- Coordinates demo execution across categories
- Delegates rendering to configured renderer
- Handles category filtering and result aggregation

#### Entry Point (`Program.cs`)
- CLI argument parsing
- Demo catalog definition (single source of truth)
- Renderer factory and runner initialization

## Usage

### Command-Line Options

| Option       | Arguments               | Default   | Description                     |
|--------------|-------------------------|-----------|---------------------------------|
| `--format`   | `console` \| `markdown` | `console` | Output format                   |
| `--category` | Category name           | *(all)*   | Run specific category only      |
| `--output`   | File path               | `demo.md` | Output file for markdown format |
| `--help`     | *(none)*                | -         | Show help message               |

### Examples

#### Console Output

```bash
# Run all demos with colored console output
dotnet run

# View specific category interactively
dotnet run -- --category "BINARY COMPARISONS"

# Explore string comparison features
dotnet run -- --category "STRING COMPARISONS"
```

**Console Output Features:**
- ✨ Color-coded sections (headers, code, output)
- 📝 Source code display with syntax
- 🎯 Clear separation between code and output
- 📊 Summary statistics at the end

#### Markdown Generation

```bash
# Generate demo.md in current directory
dotnet run -- --format markdown

# Generate with custom filename
dotnet run -- --format markdown --output examples.md

# Generate documentation for specific category
dotnet run -- --format markdown --category "LINQ OPERATIONS" --output linq-examples.md
```

**Markdown Output Features:**
- 📚 Hierarchical structure with H2/H3 headers
- 💻 Syntax-highlighted code blocks
- 📋 Blockquotes for category descriptions
- 🔗 GitHub-friendly formatting
- 📄 Auto-generated table of contents (via headers)

### Category Names

Available categories for `--category` option:

- `BASIC ASSERTIONS`
- `BINARY COMPARISONS`
- `LOGICAL OPERATORS`
- `STRING COMPARISONS`
- `COLLECTION COMPARISONS`
- `OBJECT COMPARISONS`
- `LINQ OPERATIONS`
- `SEQUENCE EQUAL`
- `ASYNC OPERATIONS`
- `DYNAMIC TYPES`
- `NULLABLE TYPES`

## Adding New Demos

### Step 1: Create Demo Method

Add a new static method to the appropriate `Demos/*.cs` file:

```csharp
// In Demos/04_StringComparisonDemos.cs
public static void CaseSensitiveComparison()
{
    var actual = "Hello";
    var expected = "hello";
    Assert(actual == expected);
}
```

### Step 2: Register Demo

Add a `DemoDefinition` to `BuildDemoCatalog()` in `Program.cs`:

```csharp
new DemoCategory(
    "04",
    "STRING COMPARISONS",
    "Single-line and multiline string diffs with character-level highlighting",
    new[]
    {
        // Existing demos...
        new DemoDefinition(
            "Case Sensitive Comparison",
            "Shows case sensitivity in string comparisons",
            "STRING COMPARISONS",
            StringComparisonDemos.CaseSensitiveComparison)
    })
```

### Step 3: Verify and Regenerate

```bash
# Test the new demo in console
dotnet run -- --category "STRING COMPARISONS"

# Regenerate documentation
dotnet run -- --format markdown
```

That's it! Your new demo is now included in all output formats automatically.

## Adding New Renderers

Want to add HTML, JSON, or another output format? Here's how:

### Step 1: Implement Renderer Interface

Create a new renderer in `Rendering/` directory:

```csharp
namespace SharpAssert.Demo.Rendering;

sealed class HtmlRenderer : IDemoRenderer, ISaveableRenderer
{
    readonly StringBuilder _html = new();

    public void RenderHeader(string title)
    {
        _html.AppendLine($"<h1>{title}</h1>");
    }

    public void RenderCategory(DemoCategory category)
    {
        _html.AppendLine($"<h2>{category.Number}. {category.Name}</h2>");
        _html.AppendLine($"<blockquote>{category.Description}</blockquote>");
    }

    public void RenderDemo(DemoDefinition demo, DemoResult result)
    {
        _html.AppendLine($"<h3>{demo.Name}</h3>");
        _html.AppendLine($"<p>{demo.Description}</p>");

        var code = demo.ExtractCode();
        _html.AppendLine("<pre><code class=\"language-csharp\">");
        _html.AppendLine(code);
        _html.AppendLine("</code></pre>");

        _html.AppendLine("<pre><code>");
        _html.AppendLine(result.Output);
        _html.AppendLine("</code></pre>");
    }

    public void RenderFooter(int categoryCount, int totalDemos)
    {
        _html.AppendLine($"<p>Total: {categoryCount} categories, {totalDemos} demos</p>");
    }

    public void Complete() { }

    public void Save(string filePath)
    {
        File.WriteAllText(filePath, _html.ToString());
    }
}
```

### Step 2: Register Renderer

Add to `CreateRenderer()` in `Program.cs`:

```csharp
static IDemoRenderer CreateRenderer(string format, string? outputFile)
{
    return format.ToLower() switch
    {
        "markdown" or "md" => new MarkdownRenderer(),
        "html" => new HtmlRenderer(),  // ← Add this
        "console" => new ConsoleRenderer(),
        _ => new ConsoleRenderer()
    };
}
```

### Step 3: Update Help Text

Add the new format to `PrintHelp()`:

```csharp
Console.WriteLine("  --format <console|markdown|html>  Output format (default: console)");
```

### Step 4: Use It

```bash
dotnet run -- --format html --output demo.html
```

## CI/CD Integration

### GitHub Actions Example

Automatically regenerate documentation when demos change:

```yaml
# .github/workflows/update-demo-docs.yml
name: Update Demo Documentation

on:
  push:
    branches: [main]
    paths:
      - 'src/SharpAssert.Demo/**'
      - 'src/SharpAssert.Runtime/**'

jobs:
  generate-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build SharpAssert
        run: dotnet build src/SharpAssert/SharpAssert.csproj

      - name: Generate demo.md
        run: |
          cd src/SharpAssert.Demo
          dotnet run -- --format markdown --output ../../demo.md

      - name: Commit updated documentation
        run: |
          git config user.name "GitHub Actions Bot"
          git config user.email "actions@github.com"
          git add demo.md
          git diff --quiet && git diff --staged --quiet || \
            (git commit -m "docs: auto-update demo documentation" && git push)
```

### Benefits of CI Integration

✅ **Zero manual work**: Docs update automatically
✅ **Always accurate**: Can't forget to regenerate
✅ **Versioned**: Documentation tracked in git
✅ **Reviewable**: Changes visible in pull requests

## Development

### Project Structure

```
SharpAssert.Demo/
├── Program.cs                      # Entry point, CLI, demo catalog
├── DemoDefinition.cs               # Demo model and execution
├── DemoCategory.cs                 # Category grouping
├── DemoRunner.cs                   # Orchestration
├── SourceCodeExtractor.cs          # Extract method source for display
├── Rendering/
│   ├── IDemoRenderer.cs            # Renderer interface
│   ├── ConsoleRenderer.cs          # Console output
│   └── MarkdownRenderer.cs         # Markdown generation
└── Demos/

```

### Building

```bash
# Build the demo project
dotnet build

# Build with verbose output
dotnet build -v detailed

# Clean and rebuild
dotnet clean && dotnet build
```

### Testing Changes

```bash
# Quick test: view in console
dotnet run -- --category "BASIC ASSERTIONS"

# Generate markdown to verify
dotnet run -- --format markdown --output test.md

# Compare with existing demo.md
diff demo.md test.md
```

### Design Principles

1. **Single Source of Truth**: Demos are defined once in `BuildDemoCatalog()`
2. **Separation of Concerns**: Execution (DemoRunner) separate from rendering (IDemoRenderer)
3. **Fail Fast**: All demos intentionally fail to showcase diagnostic output
4. **Extensibility**: Easy to add new demos and renderers
5. **No Magic**: Explicit registration, clear data flow

## Future Enhancements

### Planned Features

- **HTML Renderer**: Rich web-based showcase with syntax highlighting
- **JSON Renderer**: Structured output for tooling integration
- **Watch Mode**: Auto-regenerate on file changes (`--watch`)
- **Diff Mode**: Show what changed between versions (`--diff-with v1.0.0`)
- **Category Templates**: Generate partial docs for specific use cases
- **Performance Metrics**: Track demo execution time

### Contribution Ideas

- Add interactive HTML output with collapsible sections
- Implement PDF generation via Markdown → PDF pipeline
- Create Jupyter notebook renderer for interactive exploration
- Add screenshot capture for visual regression testing
- Generate comparison tables between different assertion libraries

## Troubleshooting

### Demos Not Updating

```bash
# Clean and rebuild
dotnet clean
dotnet build

# Force regeneration
dotnet run -- --format markdown
```

### Source Code Not Extracting

The `SourceCodeExtractor` looks for files matching `*{ClassName}.cs` pattern. Ensure:
- Demo class files follow naming convention (e.g., `01_BasicAssertionsDemos.cs`)
- Demo methods are `public static`
- Files are in the `Demos/` directory

### Category Not Found

Category names are **case-sensitive** and must match exactly:

```bash
# ❌ Wrong (lowercase)
dotnet run -- --category "string comparisons"

# ✅ Correct (uppercase)
dotnet run -- --category "STRING COMPARISONS"
```

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Related

- [SharpAssert Main Documentation](../../README.md)
- [Generated Demo Output](./demo.md)
- [Contributing Guide](../../CONTRIBUTING.md)