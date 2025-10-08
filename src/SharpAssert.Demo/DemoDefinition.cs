namespace SharpAssert.Demo;

sealed class DemoDefinition(
    string name,
    string description,
    string category,
    Delegate demoMethod)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Category { get; } = category;
    public Delegate DemoMethod { get; } = demoMethod;
    public bool IsAsync { get; } = demoMethod is Func<Task>;

    public string ExtractCode()
    {
        return SourceCodeExtractor.ExtractMethodSource(DemoMethod);
    }

    public async Task<DemoResult> ExecuteAsync()
    {
        try
        {
            if (DemoMethod is Action action)
            {
                action();
            }
            else if (DemoMethod is Func<Task> asyncAction)
            {
                await asyncAction();
            }
            else
            {
                return DemoResult.Error($"Unsupported delegate type: {DemoMethod.GetType()}");
            }

            return DemoResult.Error("Demo should have thrown SharpAssertionException but didn't");
        }
        catch (SharpAssertionException ex)
        {
            return DemoResult.Success(ex.Message);
        }
        catch (Exception ex)
        {
            return DemoResult.Error($"Unexpected exception: {ex.GetType().Name}\n{ex.Message}");
        }
    }
}

sealed class DemoResult
{
    public bool IsSuccess { get; }
    public string Output { get; }

    DemoResult(bool isSuccess, string output)
    {
        IsSuccess = isSuccess;
        Output = output;
    }

    public static DemoResult Success(string output) => new(true, output);
    public static DemoResult Error(string message) => new(false, message);
}
