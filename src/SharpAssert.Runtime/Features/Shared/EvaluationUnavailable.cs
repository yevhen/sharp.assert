namespace SharpAssert.Features.Shared;

public sealed record EvaluationUnavailable(string Reason)
{
    public override string ToString() => $"<unavailable: {Reason}>";
}
