using SharpAssert.Demo.Demos;
using SharpAssert.Demo.Rendering;

namespace SharpAssert.Demo;

static class Program
{
    const string DefaultOutputFile = "demo.md";

    static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);
        var renderer = CreateRenderer(options.Format, options.OutputFile);
        var categories = BuildDemoCatalog();
        var runner = new DemoRunner(renderer);

        if (options.Category != null)
        {
            await runner.RunCategoryAsync(options.Category, categories);
            return 0;
        }

        await runner.RunAllAsync("SharpAssert Demo Output", categories);
        return 0;
    }

    static IDemoRenderer CreateRenderer(string format, string? outputFile)
    {
        return format.ToLower() switch
        {
            "markdown" or "md" => new MarkdownRenderer(outputFile ?? DefaultOutputFile),
            "console" => new ConsoleRenderer(),
            _ => new ConsoleRenderer()
        };
    }

    static IEnumerable<DemoCategory> BuildDemoCatalog()
    {
        return new[]
        {
            new DemoCategory(
                "01",
                "BASIC ASSERTIONS",
                "Fundamental assertion failures showing expression text and values",
                new[]
                {
                    new DemoDefinition(
                        "Simple Failure",
                        "Basic assertion failure with expression text",
                        "BASIC ASSERTIONS",
                        BasicAssertionsDemos.SimpleFailure),
                    new DemoDefinition(
                        "Expression Text",
                        "Shows operands and result for comparison expressions",
                        "BASIC ASSERTIONS",
                        BasicAssertionsDemos.ExpressionText),
                    new DemoDefinition(
                        "Custom Message",
                        "Assertion with custom failure message",
                        "BASIC ASSERTIONS",
                        BasicAssertionsDemos.CustomMessage),
                    new DemoDefinition(
                        "Complex Expression",
                        "Multi-variable expression with operators",
                        "BASIC ASSERTIONS",
                        BasicAssertionsDemos.ComplexExpression)
                }),

            new DemoCategory(
                "02",
                "BINARY COMPARISONS",
                "Equality, inequality, and relational operator comparisons",
                new[]
                {
                    new DemoDefinition(
                        "Equality Operators",
                        "== and != showing left and right values",
                        "BINARY COMPARISONS",
                        BinaryComparisonDemos.EqualityOperators),
                    new DemoDefinition(
                        "Relational Operators",
                        "<, <=, >, >= with numbers",
                        "BINARY COMPARISONS",
                        BinaryComparisonDemos.RelationalOperators),
                    new DemoDefinition(
                        "Null Comparisons",
                        "null vs non-null showing both sides",
                        "BINARY COMPARISONS",
                        BinaryComparisonDemos.NullComparisons),
                    new DemoDefinition(
                        "Single Evaluation",
                        "Method calls happen only once",
                        "BINARY COMPARISONS",
                        BinaryComparisonDemos.SingleEvaluationDemo),
                    new DemoDefinition(
                        "Type Mismatch",
                        "Comparing different types",
                        "BINARY COMPARISONS",
                        BinaryComparisonDemos.TypeMismatch)
                }),

            new DemoCategory(
                "03",
                "LOGICAL OPERATORS",
                "AND, OR, NOT operators with short-circuit evaluation",
                new[]
                {
                    new DemoDefinition(
                        "AND Failure",
                        "Shows which operand failed",
                        "LOGICAL OPERATORS",
                        LogicalOperatorDemos.AndOperatorFailure),
                    new DemoDefinition(
                        "Short-Circuit AND",
                        "Right side not evaluated when left is false",
                        "LOGICAL OPERATORS",
                        LogicalOperatorDemos.ShortCircuitAnd),
                    new DemoDefinition(
                        "OR Failure",
                        "Both operands evaluated and shown",
                        "LOGICAL OPERATORS",
                        LogicalOperatorDemos.OrOperatorFailure),
                    new DemoDefinition(
                        "NOT Operator",
                        "Shows the actual value being negated",
                        "LOGICAL OPERATORS",
                        LogicalOperatorDemos.NotOperator),
                    new DemoDefinition(
                        "Nested Logical",
                        "Complex expressions with multiple operators",
                        "LOGICAL OPERATORS",
                        LogicalOperatorDemos.NestedLogical)
                }),

            new DemoCategory(
                "04",
                "STRING COMPARISONS",
                "Single-line and multiline string diffs with character-level highlighting",
                new[]
                {
                    new DemoDefinition(
                        "Single-Line Diff",
                        "Inline character diff for single-line strings",
                        "STRING COMPARISONS",
                        StringComparisonDemos.SingleLineInlineDiff),
                    new DemoDefinition(
                        "Multiline Diff",
                        "Line-by-line comparison for multiline strings",
                        "STRING COMPARISONS",
                        StringComparisonDemos.MultilineLineDiff),
                    new DemoDefinition(
                        "Null vs String",
                        "null compared to non-null string",
                        "STRING COMPARISONS",
                        StringComparisonDemos.NullVsString),
                    new DemoDefinition(
                        "Empty vs Non-Empty",
                        "Empty string vs text",
                        "STRING COMPARISONS",
                        StringComparisonDemos.EmptyVsNonEmpty),
                    new DemoDefinition(
                        "Long Strings",
                        "Long text with diff and truncation",
                        "STRING COMPARISONS",
                        StringComparisonDemos.LongStrings)
                }),

            new DemoCategory(
                "05",
                "COLLECTION COMPARISONS",
                "Array and list comparisons showing differences, missing, and extra elements",
                new[]
                {
                    new DemoDefinition(
                        "First Mismatch",
                        "Shows first element that differs",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.FirstMismatch),
                    new DemoDefinition(
                        "Missing Elements",
                        "Shows elements present in expected but not actual",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.MissingElements),
                    new DemoDefinition(
                        "Extra Elements",
                        "Shows elements in actual but not expected",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.ExtraElements),
                    new DemoDefinition(
                        "Empty Collection",
                        "Empty vs non-empty collection",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.EmptyCollection),
                    new DemoDefinition(
                        "Different Lengths",
                        "Collections with different sizes",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.DifferentLengths),
                    new DemoDefinition(
                        "Large Collections",
                        "Preview truncation for large collections",
                        "COLLECTION COMPARISONS",
                        CollectionComparisonDemos.LargeCollections)
                }),

            new DemoCategory(
                "06",
                "OBJECT COMPARISONS",
                "Deep object comparison showing property-level differences",
                new[]
                {
                    new DemoDefinition(
                        "Property Difference",
                        "Simple object with different property values",
                        "OBJECT COMPARISONS",
                        ObjectComparisonDemos.SimplePropertyDifference),
                    new DemoDefinition(
                        "Nested Objects",
                        "Deep property paths like Address.City",
                        "OBJECT COMPARISONS",
                        ObjectComparisonDemos.NestedObjectDifference),
                    new DemoDefinition(
                        "Null Object",
                        "null vs object instance",
                        "OBJECT COMPARISONS",
                        ObjectComparisonDemos.NullObjectComparison),
                    new DemoDefinition(
                        "Record Comparison",
                        "Record type value equality",
                        "OBJECT COMPARISONS",
                        ObjectComparisonDemos.RecordComparison),
                    new DemoDefinition(
                        "Multiple Differences",
                        "Multiple properties differ",
                        "OBJECT COMPARISONS",
                        ObjectComparisonDemos.MultiplePropertyDifferences)
                }),

            new DemoCategory(
                "07",
                "LINQ OPERATIONS",
                "Contains, Any, All with predicates showing matching/failing items",
                new[]
                {
                    new DemoDefinition(
                        "Contains Failure",
                        "Shows collection contents when item not found",
                        "LINQ OPERATIONS",
                        LinqOperationDemos.ContainsFailure),
                    new DemoDefinition(
                        "Any with Predicate",
                        "Shows items matching predicate",
                        "LINQ OPERATIONS",
                        LinqOperationDemos.AnyWithPredicate),
                    new DemoDefinition(
                        "All with Predicate",
                        "Shows items failing predicate",
                        "LINQ OPERATIONS",
                        LinqOperationDemos.AllWithPredicate),
                    new DemoDefinition(
                        "Empty Collection",
                        "LINQ on empty collection",
                        "LINQ OPERATIONS",
                        LinqOperationDemos.EmptyCollectionLinq)
                }),

            new DemoCategory(
                "08",
                "SEQUENCE EQUAL",
                "SequenceEqual with unified diff display",
                new[]
                {
                    new DemoDefinition(
                        "Unified Diff",
                        "Side-by-side sequence comparison",
                        "SEQUENCE EQUAL",
                        SequenceEqualDemos.UnifiedDiffDisplay),
                    new DemoDefinition(
                        "Different Lengths",
                        "Sequences of different sizes",
                        "SEQUENCE EQUAL",
                        SequenceEqualDemos.DifferentLengths),
                    new DemoDefinition(
                        "Element-by-Element",
                        "Detailed element comparison",
                        "SEQUENCE EQUAL",
                        SequenceEqualDemos.ElementByElement),
                    new DemoDefinition(
                        "Large Sequences",
                        "Truncation for large sequences",
                        "SEQUENCE EQUAL",
                        SequenceEqualDemos.LargeSequences)
                }),

            new DemoCategory(
                "09",
                "ASYNC OPERATIONS",
                "Async/await assertions showing awaited values",
                new[]
                {
                    new DemoDefinition(
                        "Basic Await",
                        "Simple async condition",
                        "ASYNC OPERATIONS",
                        AsyncDemos.BasicAwaitCondition),
                    new DemoDefinition(
                        "Async Binary",
                        "Both sides awaited with values shown",
                        "ASYNC OPERATIONS",
                        AsyncDemos.AsyncBinaryComparison),
                    new DemoDefinition(
                        "Mixed Async/Sync",
                        "Await on one side, constant on other",
                        "ASYNC OPERATIONS",
                        AsyncDemos.MixedAsyncSync),
                    new DemoDefinition(
                        "Async String Diff",
                        "Awaited string with diff",
                        "ASYNC OPERATIONS",
                        AsyncDemos.AsyncStringDiff)
                }),

            new DemoCategory(
                "10",
                "DYNAMIC TYPES",
                "Dynamic type assertions with DLR evaluation",
                new[]
                {
                    new DemoDefinition(
                        "Dynamic Binary",
                        "Dynamic comparison showing values",
                        "DYNAMIC TYPES",
                        DynamicDemos.DynamicBinaryComparison),
                    new DemoDefinition(
                        "Dynamic Method Call",
                        "Method call on dynamic object",
                        "DYNAMIC TYPES",
                        DynamicDemos.DynamicMethodCall),
                    new DemoDefinition(
                        "Dynamic Operators",
                        "Dynamic operator semantics",
                        "DYNAMIC TYPES",
                        DynamicDemos.DynamicOperatorSemantics)
                }),

            new DemoCategory(
                "11",
                "NULLABLE TYPES",
                "Nullable value types and reference types showing HasValue and Value",
                new[]
                {
                    new DemoDefinition(
                        "Nullable Int (null)",
                        "int? null showing HasValue: false",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullableIntWithNull),
                    new DemoDefinition(
                        "Nullable Int (value)",
                        "int? with value showing HasValue: true, Value",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullableIntWithValue),
                    new DemoDefinition(
                        "Nullable Bool",
                        "bool? comparison",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullableBool),
                    new DemoDefinition(
                        "Nullable DateTime",
                        "DateTime? null comparison",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullableDateTime),
                    new DemoDefinition(
                        "Nullable Reference Types",
                        "string?, object? comparisons",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullableReferenceTypes),
                    new DemoDefinition(
                        "Null Comparison Edge Cases",
                        "nullable == null",
                        "NULLABLE TYPES",
                        NullableTypeDemos.NullComparisonEdgeCases)
                })
        };
    }

    static CommandLineOptions ParseArguments(string[] args)
    {
        var format = "console";
        string? category = null;
        string? outputFile = DefaultOutputFile;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format" when i + 1 < args.Length:
                    format = args[++i];
                    break;
                case "--category" when i + 1 < args.Length:
                    category = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--help":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return new CommandLineOptions(format, category, outputFile);
    }

    static void PrintHelp()
    {
        Console.WriteLine("SharpAssert Demo Runner");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --format <console|markdown>  Output format (default: console)");
        Console.WriteLine("  --category <name>            Run specific category only");
        Console.WriteLine("  --output <file>              Output file for markdown format (default: demo.md)");
        Console.WriteLine("  --help                       Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                                    # Run all demos in console");
        Console.WriteLine("  dotnet run -- --format markdown               # Generate demo.md");
        Console.WriteLine("  dotnet run -- --category \"STRING COMPARISONS\" # Run only string demos");
        Console.WriteLine("  dotnet run -- --format markdown --output out.md  # Custom output file");
    }

    record CommandLineOptions(string Format, string? Category, string? OutputFile);
}
