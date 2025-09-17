namespace SharpAssert;

/// <summary>
/// Encapsulates a value and its type information for assertion comparison operations.
/// </summary>
/// <param name="Value">The runtime value of the operand, which may be null.</param>
/// <param name="ExpressionType">The compile-time type of the operand taken from expression.</param>
public record AssertionOperand(object? Value, Type? ExpressionType = null)
{
    /// <summary>
    /// Gets a value indicating whether this operand represents a nullable value type.
    /// </summary>
    public bool IsNullableValueType
    {
        get
        {
            if (ExpressionType == null)
                return false;
            return ExpressionType.IsGenericType && ExpressionType?.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this operand's value is null.
    /// </summary>
    public bool IsNull => Value is null;

    /// <summary>
    /// Gets the underlying type, unwrapping nullable types to their base type.
    /// </summary>
    public Type? UnderlyingType => ExpressionType != null && IsNullableValueType ? Nullable.GetUnderlyingType(ExpressionType!) : ExpressionType;
}