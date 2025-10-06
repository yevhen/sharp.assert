namespace SharpAssert;

class TestObject
{
    public bool IsValid { get; set; }
    
    public bool GetValidationResult()
    {
        return IsValid;
    }
}

class NonComparableClass
{
    public string Name { get; set; } = "";
    
    public override string ToString() => Name;
}

class DifferentNonComparableClass
{
    public int Value { get; set; }
    
    public override string ToString() => Value.ToString();
}