namespace SharpAssert.Features.Shared;

abstract record ComparisonResult(AssertionOperand Left, AssertionOperand Right)
{
    public abstract IReadOnlyList<RenderedLine> Render();
}
