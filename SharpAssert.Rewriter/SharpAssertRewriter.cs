using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert.Rewriter;

public static class SharpAssertRewriter
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

        // Ensure we use absolute path for #line directives
        var absoluteFileName = Path.IsPathRooted(fileName) ? fileName : Path.GetFullPath(fileName);
        
        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, absoluteFileName);
        var rewrittenRoot = rewriter.Visit(root);

        // Only add #line directive if there were actual rewrites
        if (rewriter.HasRewrites)
        {
            var lineDirective = CreateLineDirective(1, absoluteFileName);
            var rewrittenWithLineDirective = rewrittenRoot.WithLeadingTrivia(
                SyntaxFactory.TriviaList(
                    lineDirective,
                    SyntaxFactory.EndOfLine("\n")));

            return rewrittenWithLineDirective.ToFullString();
        }

        return rewrittenRoot.ToFullString();
    }

    static MetadataReference[] CreateCompilationReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
    ];

    public static SyntaxTrivia CreateLineDirective(int lineNumber, string filePath)
    {
        return SyntaxFactory.PreprocessingMessage($"#line {lineNumber} \"{EscapeFilePath(filePath)}\"");
    }

    public static SyntaxTrivia CreateDefaultLineDirective()
    {
        return SyntaxFactory.PreprocessingMessage("#line default");
    }

    public static string EscapeFilePath(string filePath)
    {
        // Escape backslashes and quotes in file paths for #line directives
        return filePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

internal class SharpAssertSyntaxRewriter(SemanticModel semanticModel, string originalFilePath) : CSharpSyntaxRewriter
{
    public bool HasRewrites { get; private set; }
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (!IsSharpAssertCall(node))
            return base.VisitInvocationExpression(node);

        if (ContainsAwait(node))
            return base.VisitInvocationExpression(node);

        HasRewrites = true;
        return RewriteToLambda(node);
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
                        SyntaxFactory.Literal(SharpAssertRewriter.EscapeFilePath(originalFilePath)))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(lineNumber)))
                ])));
        
        // Add #line directives around the rewritten Assert
        
        return newInvocation
            .WithLeadingTrivia(
                node.GetLeadingTrivia()
                    .Add(SharpAssertRewriter.CreateLineDirective(lineNumber, originalFilePath))
                    .Add(SyntaxFactory.EndOfLine("\n")))
            .WithTrailingTrivia(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.EndOfLine("\n"),
                    SharpAssertRewriter.CreateDefaultLineDirective(),
                    SyntaxFactory.EndOfLine("\n"))
                .AddRange(node.GetTrailingTrivia()));
    }
}