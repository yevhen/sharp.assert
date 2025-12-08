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

    [Test]
    public void Should_display_method_call_arguments()
    {
        var objA = new NonComparableClass { Name = "A" };
        var objB = new NonComparableClass { Name = "B" };

        AssertThrows(
            () => Assert(ReferenceEquals(objA, objB)),
            "*ReferenceEquals(objA, objB)*Argument[0]:*Argument[1]:*Result: false*");
    }

    [Test]
    public void Should_display_simple_boolean_method_call_with_arguments()
    {
        var text = "Hello World";
        var prefix = "Goodbye";

        AssertThrows(
            () => Assert(text.StartsWith(prefix)),
            "*text.StartsWith(prefix)*Argument[0]: \"Goodbye\"*Result: False*");
    }

    [Test]
    public void Should_display_method_call_with_instance_and_multiple_arguments()
    {
        var text = "Hello World";

        AssertThrows(
            () => Assert(text.Contains("xyz")),
            "*Contains failed: searched for \"xyz\" in*");
    }
}