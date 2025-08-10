using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert.Rewriter;

public class SharpAssertRewriter
{
    public static string Rewrite(string source, string fileName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);

        var references = CreateCompilationReferences();

        var compilation = CSharpCompilation.Create("RewriterAnalysis")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var rewriter = new SharpAssertSyntaxRewriter(semanticModel);
        var rewrittenRoot = rewriter.Visit(root);

        return rewrittenRoot.ToFullString();
    }

    static MetadataReference[] CreateCompilationReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
    ];
}

internal class SharpAssertSyntaxRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (!IsSharpAssertCall(node))
            return base.VisitInvocationExpression(node);

        return ContainsAwait(node) ? base.VisitInvocationExpression(node) : RewriteToLambda(node);
    }

    static bool IsSharpAssertCall(InvocationExpressionSyntax node)
    {
        return node.Expression is IdentifierNameSyntax identifier &&
               identifier.Identifier.ValueText == "Assert";
    }

    static bool ContainsAwait(InvocationExpressionSyntax node)
    {
        return node.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .Any();
    }

    InvocationExpressionSyntax RewriteToLambda(InvocationExpressionSyntax node)
    {
        var argument = node.ArgumentList.Arguments[0];
        var expression = argument.Expression;
        var expressionText = expression.ToString();
        var lineNumber = semanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        
        var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(expression);
            
        var newInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("global::SharpAssert.SharpInternal"),
                SyntaxFactory.IdentifierName("Assert")))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(lambdaExpression),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression, 
                        SyntaxFactory.Literal(expressionText))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("@\"\"", ""))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(lineNumber)))
                ])));
        
        return newInvocation.WithTriviaFrom(node);
    }
}