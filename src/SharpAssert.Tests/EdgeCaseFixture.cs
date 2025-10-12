using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class EdgeCaseFixture : TestBase
{
    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        var value = "test";

        AssertThrows(() => Assert(nullString == value), "*null*test*");
    }

    [Test]
    public void Should_handle_simple_boolean_property_false()
    {
        var obj = new TestObject { IsValid = false };

        AssertThrows(() => Assert(obj.IsValid), "Assertion failed: obj.IsValid  at *:*");
    }

    [Test]
    public void Should_handle_simple_boolean_method_call_false()
    {
        var obj = new TestObject { IsValid = false };

        AssertThrows(() => Assert(obj.GetValidationResult()), "Assertion failed: obj.GetValidationResult()  at *:*");
    }

    [Test]
    public void Should_handle_boolean_constant_false()
    {
        AssertThrows(() => Assert(false), "Assertion failed: false  at *:*");
    }

    [Test]
    public void Should_handle_simple_boolean_property_true()
    {
        var obj = new TestObject { IsValid = true };

        AssertDoesNotThrow(() => Assert(obj.IsValid));
    }

    [Test]
    public void Should_handle_boolean_constant_true()
    {
        AssertDoesNotThrow(() => Assert(true));
    }

    [Test]
    public void Should_handle_reference_equality_false()
    {
        var objA = new NonComparableClass { Name = "A" };
        var objB = new DifferentNonComparableClass { Value = 10 };

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        AssertThrows(() => Assert(ReferenceEquals(objA, objB)), "*");
    }
}