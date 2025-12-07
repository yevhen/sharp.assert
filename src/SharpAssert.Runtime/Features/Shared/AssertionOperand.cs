namespace SharpAssert.Features.Shared;

record AssertionOperand(object? Value, Type? ExpressionType = null)
{
    public bool IsNullableValueType
    {
        get
        {
            if (ExpressionType == null)
                return false;
            return ExpressionType.IsGenericType && ExpressionType?.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}