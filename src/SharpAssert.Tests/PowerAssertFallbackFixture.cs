using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;

namespace SharpAssert.Tests;

[TestFixture]
public class PowerAssertFallbackFixture : TestBase
{
    [Test]
    public void Should_use_powerassert_when_force_flag_is_true()
    {
        var x = 5;
        var y = 10;
        Expression<Func<bool>> expr = () => x == y;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "x == y", "test.cs", 42, null, usePowerAssert: true, usePowerAssertForUnsupported: false));
        
        ex.Message.Should().Contain("Assert failed, expression was:", "PowerAssert should generate failure message");
    }
    
    [Test]
    public void Should_use_powerassert_for_string_comparisons_when_fallback_enabled()
    {
        var str1 = "hello";
        var str2 = "world";
        Expression<Func<bool>> expr = () => str1 == str2;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "str1 == str2", "test.cs", 42, null, usePowerAssert: false, usePowerAssertForUnsupported: true));
        
        ex.Message.Should().Contain("str1 == str2", "PowerAssert should handle string comparisons");
    }
    
    [Test]
    public void Should_use_powerassert_for_linq_contains_when_fallback_enabled()
    {
        var list = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list.Contains(5);
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "list.Contains(5)", "test.cs", 42, null, usePowerAssert: false, usePowerAssertForUnsupported: true));
        
        ex.Message.Should().Contain("list.Contains(5)", "PowerAssert should handle LINQ Contains");
    }
    
    [Test]
    public void Should_use_sharpassert_for_basic_comparisons_when_fallback_disabled()
    {
        var x = 5;
        var y = 10;
        Expression<Func<bool>> expr = () => x == y;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "x == y", "test.cs", 42, null, usePowerAssert: false, usePowerAssertForUnsupported: false));
        
        ex.Message.Should().Contain("Left:  5");
        ex.Message.Should().Contain("Right: 10");
        ex.Message.Should().Contain("Assertion failed: x == y");
    }
    
    [Test]
    public void Should_combine_custom_message_with_powerassert_output()
    {
        var x = 5;
        var y = 10;
        Expression<Func<bool>> expr = () => x == y;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "x == y", "test.cs", 42, "Custom error", usePowerAssert: true, usePowerAssertForUnsupported: false));
        
        ex.Message.Should().Contain("Custom error");
    }
    
    [Test]
    public void Should_not_use_fallback_for_string_comparisons_when_disabled()
    {
        var str1 = "hello";
        var str2 = "world";
        Expression<Func<bool>> expr = () => str1 == str2;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            SharpInternal.Assert(expr, "str1 == str2", "test.cs", 42, null, usePowerAssert: false, usePowerAssertForUnsupported: false));
        
        // Should use SharpAssert's binary comparison format
        ex.Message.Should().Contain("Left:  \"hello\"");
        ex.Message.Should().Contain("Right: \"world\"");
        ex.Message.Should().Contain("Assertion failed: str1 == str2");
    }
    
    [Test]
    public void Should_pass_when_powerassert_condition_is_true_with_force_flag()
    {
        var x = 5;
        var y = 5;
        Expression<Func<bool>> expr = () => x == y;
        
        var action = () => SharpInternal.Assert(expr, "x == y", "test.cs", 42, null, usePowerAssert: true, usePowerAssertForUnsupported: false);
        action.Should().NotThrow();
    }
    
    [Test]
    public void Should_pass_when_fallback_to_powerassert_succeeds_for_unsupported_feature()
    {
        var list = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list.Contains(2);
        
        var action = () => SharpInternal.Assert(expr, "list.Contains(2)", "test.cs", 42, null, usePowerAssert: false, usePowerAssertForUnsupported: true);
        action.Should().NotThrow();
    }
}