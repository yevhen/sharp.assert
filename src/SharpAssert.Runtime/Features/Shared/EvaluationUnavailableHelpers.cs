namespace SharpAssert.Features.Shared;

static class EvaluationUnavailableHelpers
{
    public static bool IsUnavailable(object? value) => value is EvaluationUnavailable;

    public static string DescribeUnavailable(object? value) => value is EvaluationUnavailable unavailable
        ? unavailable.ToString()
        : "<unavailable>";
}
