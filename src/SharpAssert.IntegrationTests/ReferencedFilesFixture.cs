using static SharpAssert.Sharp;

namespace SharpAssert.IntegrationTests;

[TestFixture]
public class ReferencedFilesFixture
{
    [Test]
    public void Test_can_use_code_in_non_rewritten_files()
    {
        var helper = new TestHelper();
        helper.Increment();

        Assert(helper.Count > 0);
    }
}