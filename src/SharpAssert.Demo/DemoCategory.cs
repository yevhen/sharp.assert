namespace SharpAssert.Demo;

sealed class DemoCategory(
    string number,
    string name,
    string description,
    IEnumerable<DemoDefinition> demos)
{
    public string Number { get; } = number;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public IReadOnlyList<DemoDefinition> Demos { get; } = demos.ToList();
}
