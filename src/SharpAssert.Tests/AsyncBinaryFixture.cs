namespace SharpAssert;

[TestFixture]
public class AsyncBinaryFixture : TestBase
{
    [Test]
    [Ignore("Feature not yet implemented - Increment 11")]
    public void Should_show_both_async_values()
    {
        // Assert(await Left() == await Right()) should show both operand values
        // Expected: "Left: <leftValue>, Right: <rightValue>"
        Assert.Fail("Async binary comparison not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 11")]
    public void Should_handle_mixed_async_sync()
    {
        // Assert(await AsyncMethod() == 5) should work with mixed operands
        // Expected: Mixed async/sync operand support
        Assert.Fail("Mixed async/sync binary comparison not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 11")]
    public void Should_evaluate_in_source_order()
    {
        // Assert(await First() == await Second()) should evaluate left before right
        // Expected: Operands evaluated in source order, not parallel
        Assert.Fail("Async evaluation order not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 11")]
    public void Should_apply_diffs_to_async_strings()
    {
        // Assert(await GetString1() == await GetString2()) should show string diff
        // Expected: String diffing works with async operands
        Assert.Fail("Async string diffing not yet implemented");
    }
}