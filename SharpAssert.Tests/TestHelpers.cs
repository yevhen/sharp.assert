namespace SharpAssert;

internal class TestObject
{
    public bool IsValid { get; set; }
    
    public bool GetValidationResult()
    {
        return IsValid;
    }
}

internal class NonComparableClass
{
    public string Name { get; set; } = "";
    
    public override string ToString() => Name;
}

internal class DifferentNonComparableClass
{
    public int Value { get; set; }
    
    public override string ToString() => Value.ToString();
}