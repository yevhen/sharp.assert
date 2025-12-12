using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features;

[TestFixture]
public class ExpressionValueEvaluatorFixture
{
    [Test]
    public void Evaluate_should_return_unavailable_for_unknown_byref_like_generic()
    {
        var method = GetPrivateStaticMethod(nameof(GetRefLikeGeneric));
        var expression = Expression.Call(method);

        var result = ExpressionValueEvaluator.Evaluate(expression);

        result.Should().BeOfType<EvaluationUnavailable>();
        ((EvaluationUnavailable)result!).Reason.Should().Contain("ByRef-like");
    }

    [Test]
    public void Evaluate_should_return_unavailable_when_invocation_throws()
    {
        var method = GetPrivateStaticMethod(nameof(ThrowOnInvoke));
        var expression = Expression.Call(method);

        var result = ExpressionValueEvaluator.Evaluate(expression);

        result.Should().BeOfType<EvaluationUnavailable>();
        ((EvaluationUnavailable)result!).Reason.Should().Contain(nameof(InvalidOperationException));
    }

    static MethodInfo GetPrivateStaticMethod(string name) =>
        typeof(ExpressionValueEvaluatorFixture).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    static RefLike<int> GetRefLikeGeneric() => default;

    static int ThrowOnInvoke() => throw new InvalidOperationException("boom");

    ref struct RefLike<T>
    {
        public T Value { get; }

        public RefLike(T value) => Value = value;
    }
}

