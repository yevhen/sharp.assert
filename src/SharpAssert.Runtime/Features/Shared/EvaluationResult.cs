namespace SharpAssert.Features.Shared;

record RenderedLine(int IndentLevel, string Text);

abstract record EvaluationResult(string ExpressionText)
{
    public virtual bool? BooleanValue => null;
    public abstract IReadOnlyList<RenderedLine> Render();
}
