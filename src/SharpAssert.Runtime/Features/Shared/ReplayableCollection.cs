namespace SharpAssert.Features.Shared;

static class ReplayableCollection
{
    public static bool IsKnownReplayable(object? value) =>
        value is string or Array ||
        value?.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(List<>);
}
