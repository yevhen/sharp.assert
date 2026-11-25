// ABOUTME: Evaluates comparison operators on boxed values with C# operator semantics.
// ABOUTME: Uses dynamic binding, so numbers of different types and user-defined operators behave as in compiled code.

using SharpAssert.Core;

namespace SharpAssert.Features.Shared;

static class NativeOperators
{
    public static bool Evaluate(BinaryOp op, object? leftValue, object? rightValue)
    {
        try
        {
            return op switch
            {
                BinaryOp.Eq => (dynamic?)leftValue == (dynamic?)rightValue,
                BinaryOp.Ne => (dynamic?)leftValue != (dynamic?)rightValue,
                BinaryOp.Lt => (dynamic?)leftValue < (dynamic?)rightValue,
                BinaryOp.Le => (dynamic?)leftValue <= (dynamic?)rightValue,
                BinaryOp.Gt => (dynamic?)leftValue > (dynamic?)rightValue,
                BinaryOp.Ge => (dynamic?)leftValue >= (dynamic?)rightValue,
                _ => false
            };
        }
        catch
        {
            // If dynamic operation fails, fall back to false (comparison failed)
            return false;
        }
    }
}
