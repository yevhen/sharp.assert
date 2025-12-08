namespace SharpAssert.Features.Shared;

public record RenderedLine(int IndentLevel, string Text);

public abstract record EvaluationResult(string ExpressionText)
{
    public virtual bool? BooleanValue => null;
    public abstract IReadOnlyList<RenderedLine> Render();
}
