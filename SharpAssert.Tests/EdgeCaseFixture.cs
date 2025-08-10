using System.Linq.Expressions;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class EdgeCaseFixture : TestBase
{
    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        var value = "test";
        Expression<Func<bool>> expr = () => nullString == value;

        AssertExpressionThrows<SharpAssertionException>(expr, "nullString == value", "TestFile.cs", 100, "*null*test*");
    }

    [Test]
    public void Should_handle_simple_boolean_property_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.IsValid;

        AssertExpressionThrows<SharpAssertionException>(expr, "obj.IsValid", "TestFile.cs", 300, "Assertion failed: obj.IsValid  at TestFile.cs:300");
    }

    [Test]
    public void Should_handle_simple_boolean_method_call_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.GetValidationResult();

        AssertExpressionThrows<SharpAssertionException>(expr, "obj.GetValidationResult()", "TestFile.cs", 301, "Assertion failed: obj.GetValidationResult()  at TestFile.cs:301");
    }

    [Test]
    public void Should_handle_boolean_constant_false()
    {
        Expression<Func<bool>> expr = () => false;

        AssertExpressionThrows<SharpAssertionException>(expr, "false", "TestFile.cs", 302, "Assertion failed: false  at TestFile.cs:302");
    }

    [Test]
    public void Should_handle_simple_boolean_property_true()
    {
        var obj = new TestObject { IsValid = true };
        Expression<Func<bool>> expr = () => obj.IsValid;

        AssertExpressionDoesNotThrow(expr, "obj.IsValid", "TestFile.cs", 303);
    }

    [Test]
    public void Should_handle_boolean_constant_true()
    {
        Expression<Func<bool>> expr = () => true;

        AssertExpressionDoesNotThrow(expr, "true", "TestFile.cs", 304);
    }

    [Test]
    public void Should_handle_reference_equality_false()
    {
        var objA = new NonComparableClass { Name = "A" };
        var objB = new DifferentNonComparableClass { Value = 10 };

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        Expression<Func<bool>> expr = () => ReferenceEquals(objA, objB);

        AssertExpressionThrows<SharpAssertionException>(expr, "ReferenceEquals(objA, objB)", "TestFile.cs", 401, "*");
    }
}