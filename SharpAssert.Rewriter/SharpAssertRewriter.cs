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
        
        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, absoluteFileName, fileName);
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

    public static SyntaxTrivia CreateLineDirective(int lineNumber, string filePath) =>
        SyntaxFactory.PreprocessingMessage($"#line {lineNumber} \"{EscapeFilePath(filePath)}\"");

    public static SyntaxTrivia CreateDefaultLineDirective() =>
        SyntaxFactory.PreprocessingMessage("#line default");

    public static string EscapeFilePath(string filePath) =>
        // Escape backslashes and quotes in file paths for #line directives
        filePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

internal class SharpAssertSyntaxRewriter(SemanticModel semanticModel, string absoluteFileName, string fileName) : CSharpSyntaxRewriter
{
    public bool HasRewrites { get; private set; }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (!IsSharpAssertCall(node) || ContainsAwait(node))
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
        var rewriteData = ExtractRewriteData(node);
        var lambdaExpression = CreateLambdaExpression(rewriteData.Expression);
        var newInvocation = CreateSharpInternalInvocation(lambdaExpression, rewriteData);
        return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
    }

    RewriteData ExtractRewriteData(InvocationExpressionSyntax node)
    {
        const int lineNumberOffset = 1;

        var conditionArgument = node.ArgumentList.Arguments[0];
        var expression = conditionArgument.Expression;
        var expressionText = expression.ToString();
        var lineNumber = semanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + lineNumberOffset;
        
        ExpressionSyntax? messageExpression = null;
        if (node.ArgumentList.Arguments.Count > 1)
            messageExpression = node.ArgumentList.Arguments[1].Expression;
        
        return new RewriteData(expression, expressionText, lineNumber, messageExpression);
    }

    static ParenthesizedLambdaExpressionSyntax CreateLambdaExpression(ExpressionSyntax expression)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(expression);
    }

    InvocationExpressionSyntax CreateSharpInternalInvocation(ParenthesizedLambdaExpressionSyntax lambdaExpression, RewriteData data)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("global::SharpAssert.SharpInternal"),
                SyntaxFactory.IdentifierName("Assert")))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(lambdaExpression),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression, 
                        SyntaxFactory.Literal(data.ExpressionText))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(fileName))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(data.LineNumber))),
                    SyntaxFactory.Argument(data.MessageExpression ?? SyntaxFactory.LiteralExpression(
                        SyntaxKind.NullLiteralExpression,
                        SyntaxFactory.Token(SyntaxKind.NullKeyword)))
                ])));
    }

    InvocationExpressionSyntax AddLineDirectives(InvocationExpressionSyntax invocation, InvocationExpressionSyntax originalNode, int lineNumber)
    {
        return invocation
            .WithLeadingTrivia(
                originalNode.GetLeadingTrivia()
                    .Add(SharpAssertRewriter.CreateLineDirective(lineNumber, absoluteFileName))
                    .Add(SyntaxFactory.EndOfLine("\n")))
            .WithTrailingTrivia(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.EndOfLine("\n"),
                    SharpAssertRewriter.CreateDefaultLineDirective(),
                    SyntaxFactory.EndOfLine("\n"))
                .AddRange(originalNode.GetTrailingTrivia()));
    }

    record RewriteData(ExpressionSyntax Expression, string ExpressionText, int LineNumber, ExpressionSyntax? MessageExpression);
}