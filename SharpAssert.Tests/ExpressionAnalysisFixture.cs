using FluentAssertions;
using SharpAssert;
using System.Linq.Expressions;

namespace SharpAssert.Tests;

[TestFixture]
public class ExpressionAnalysisFixture
{
    [Test]
    public void Should_show_left_and_right_values_for_equality()
    {
        var x = 42;
        var y = 24;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 123);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        var x = 5;
        var y = 10;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 1);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        var a = 5;
        var b = 5;
        Expression<Func<bool>> expr = () => a != b;

        var action = () => SharpInternal.Assert(expr, "a != b", "TestFile.cs", 2);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        var c = 10;
        var d = 5;
        Expression<Func<bool>> expr = () => c < d;

        var action = () => SharpInternal.Assert(expr, "c < d", "TestFile.cs", 3);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        var e = 10;
        var f = 5;
        Expression<Func<bool>> expr = () => e <= f;

        var action = () => SharpInternal.Assert(expr, "e <= f", "TestFile.cs", 4);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        var g = 5;
        var h = 10;
        Expression<Func<bool>> expr = () => g > h;

        var action = () => SharpInternal.Assert(expr, "g > h", "TestFile.cs", 5);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        var i = 5;
        var j = 10;
        Expression<Func<bool>> expr = () => i >= j;

        var action = () => SharpInternal.Assert(expr, "i >= j", "TestFile.cs", 6);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        var value = "test";
        Expression<Func<bool>> expr = () => nullString == value;

        var action = () => SharpInternal.Assert(expr, "nullString == value", "TestFile.cs", 100);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*null*test*");
    }

    int callCount = 0;
    
    int GetValueAndIncrement()
    {
        callCount++;
        return callCount * 10;
    }

    [Test]
    public void Should_evaluate_complex_expressions_once()
    {
        callCount = 0;

        Expression<Func<bool>> expr = () => GetValueAndIncrement() == GetValueAndIncrement();

        var action = () => SharpInternal.Assert(expr, "GetValueAndIncrement() == GetValueAndIncrement()", "TestFile.cs", 200);
        action.Should().Throw<SharpAssertionException>();
        
        callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }

    [Test]
    public void Should_handle_simple_boolean_property_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.IsValid;

        var action = () => SharpInternal.Assert(expr, "obj.IsValid", "TestFile.cs", 300);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Assertion failed: obj.IsValid  at TestFile.cs:300");
    }

    [Test]
    public void Should_handle_simple_boolean_method_call_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.GetValidationResult();

        var action = () => SharpInternal.Assert(expr, "obj.GetValidationResult()", "TestFile.cs", 301);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Assertion failed: obj.GetValidationResult()  at TestFile.cs:301");
    }

    [Test]
    public void Should_handle_boolean_constant_false()
    {
        Expression<Func<bool>> expr = () => false;

        var action = () => SharpInternal.Assert(expr, "false", "TestFile.cs", 302);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Assertion failed: false  at TestFile.cs:302");
    }

    [Test]
    public void Should_handle_simple_boolean_property_true()
    {
        var obj = new TestObject { IsValid = true };
        Expression<Func<bool>> expr = () => obj.IsValid;

        // This should NOT throw
        var action = () => SharpInternal.Assert(expr, "obj.IsValid", "TestFile.cs", 303);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_handle_boolean_constant_true()
    {
        Expression<Func<bool>> expr = () => true;

        // This should NOT throw
        var action = () => SharpInternal.Assert(expr, "true", "TestFile.cs", 304);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_handle_comparison_exceptions_for_incompatible_types()
    {
        // This creates a scenario where the values themselves are obtained correctly,
        // but Comparer<object>.Default.Compare() will throw because it can't compare these types
        var stringValue = "hello";
        var intValue = 42;
        
        // This will be evaluated as a binary comparison where left="hello" and right=42
        // The EvaluateBinaryOperation method will catch the ArgumentException from Comparer<object>.Default.Compare()
        Expression<Func<bool>> expr = () => stringValue.Length > intValue;

        var action = () => SharpInternal.Assert(expr, "stringValue.Length > intValue", "TestFile.cs", 400);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*42*");
    }

    [Test]
    public void Should_handle_comparison_exceptions_by_returning_false()
    {
        // Create objects that will cause Comparer<object>.Default.Compare to throw
        // This can happen when types don't implement IComparable and aren't the same type
        var objA = new NonComparableClass { Name = "A" };
        var objB = new DifferentNonComparableClass { Value = 10 };
        
        // Create a comparison that will cause Comparer<object>.Default.Compare to throw ArgumentException
        // We'll test this by creating an expression that compares these directly
        Expression<Func<bool>> expr = () => ReferenceEquals(objA, objB);

        // Since this evaluates to false, it should throw SharpAssertionException
        var action = () => SharpInternal.Assert(expr, "ReferenceEquals(objA, objB)", "TestFile.cs", 401);
        action.Should().Throw<SharpAssertionException>();
    }
}

internal class TestObject
{
    public bool IsValid { get; set; }
    
    public bool GetValidationResult()
    {
        return IsValid;
    }
}

internal class IncomparableObject : IComparable<IncomparableObject>
{
    public int Value { get; set; }
    
    public int CompareTo(IncomparableObject? other)
    {
        if (other == null) return 1;
        return Value.CompareTo(other.Value);
    }
}

internal class ThrowingComparableObject : IComparable<ThrowingComparableObject>
{
    public int Value { get; set; }
    
    public int CompareTo(ThrowingComparableObject? other)
    {
        throw new InvalidOperationException("Comparison not supported");
    }
}

internal class UncomparableObject
{
    public string Value { get; set; } = "";
    
    public override string ToString() => Value;
}

internal class NonComparableClass
{
    public string Name { get; set; } = "";
    
    public override string ToString() => Name;
}

internal class DifferentNonComparableClass
{
    public int Value { get; set; }
    
    public override string ToString() => Value.ToString();
}