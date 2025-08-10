using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class ConfigurationFixture : TestBase
{
    [Test]
    [Ignore("Feature not yet implemented - Increment 13")]
    public void Should_use_default_options()
    {
        // SharpConfig.Current should return default values
        // Expected: Default configuration values applied
        Assert.Fail("Configuration system not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 13")]
    public void Should_override_with_scoped_options()
    {
        // using (SharpConfig.WithOptions(new SharpOptions {...})) should change settings
        // Expected: Scoped configuration changes take effect
        Assert.Fail("Scoped configuration not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 13")]
    public void Should_restore_after_scope()
    {
        // Original settings should be restored after using block
        // Expected: Configuration restored to previous state
        Assert.Fail("Configuration restoration not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 13")]
    public void Should_isolate_parallel_test_configs()
    {
        // Parallel tests should not interfere with each other's config
        // Expected: AsyncLocal isolation works correctly
        Assert.Fail("Parallel configuration isolation not yet implemented");
    }
}