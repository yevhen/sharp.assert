using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace SharpAssert;

[TestFixture]
public class ExceptionConversionFixture
{
    [Test]
    public async Task Should_return_the_captured_exception_after_async_conversion()
    {
        var expected = new Exception("failure");

        Exception actual = await Sharp.ThrowsAsync<Exception>(() => Task.FromException(expected));

        actual.Should().BeSameAs(expected);
    }

    [Test]
    public void Should_return_the_captured_derived_exception_as_a_base_exception()
    {
        var expected = new ArgumentException("failure");

        Exception actual = Sharp.Throws<ArgumentException>(() => throw expected);

        actual.Should().BeSameAs(expected);
    }

    [TestCase("ArgumentException specific = Throws<ArgumentException>(() => throw new ArgumentException());")]
    [TestCase("Exception general = Throws<ArgumentException>(() => throw new ArgumentException());")]
    [TestCase("Exception exact = Throws<Exception>(() => throw new Exception());")]
    [TestCase("Exception failure = await ThrowsAsync<Exception>(() => Task.FromException(new Exception()));")]
    public void Should_compile_exception_assignments(string assignment)
    {
        var source = $$"""
            using System;
            using System.Threading.Tasks;
            using static SharpAssert.Sharp;
            public class Sample
            {
                public static async Task Check()
                {
                    {{assignment}}
                    await Task.CompletedTask;
                }
            }
            """;

        RewrittenSource.Compile(source).GetDiagnostics()
            .Where(each => each.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().BeEmpty();
    }
}
