using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert.Rewriter;

public class SharpAssertRewriter
{
    public string Rewrite(string source, string fileName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);
        
        // Add basic references needed for compilation
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
        };
        
        var compilation = CSharpCompilation.Create("RewriterAnalysis")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);
        
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();
        
        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, fileName);
        var rewrittenRoot = rewriter.Visit(root);
        
        return rewrittenRoot.ToFullString();
    }
}

internal class SharpAssertSyntaxRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly string _fileName;
    
    public SharpAssertSyntaxRewriter(SemanticModel semanticModel, string fileName)
    {
        _semanticModel = semanticModel;
        _fileName = fileName;
    }
    
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (IsSharpAssertCall(node))
        {
            if (ContainsAwait(node))
                return base.VisitInvocationExpression(node); // Skip rewriting async cases
                
            return RewriteToLambda(node);
        }
        
        return base.VisitInvocationExpression(node);
    }
    
    private bool IsSharpAssertCall(InvocationExpressionSyntax node)
    {
        // Simple pattern matching for Assert calls
        return node.Expression is IdentifierNameSyntax identifier && 
               identifier.Identifier.ValueText == "Assert";
    }
    
    private bool ContainsAwait(InvocationExpressionSyntax node)
    {
        return node.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .Any();
    }
    
    private SyntaxNode RewriteToLambda(InvocationExpressionSyntax node)
    {
        var argument = node.ArgumentList.Arguments[0];
        var expression = argument.Expression;
        var expressionText = expression.ToString();
        var lineNumber = _semanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        
        var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(expression);
            
        var newInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("global::SharpInternal"),
                SyntaxFactory.IdentifierName("Assert")))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
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
                })));
        
        return newInvocation.WithTriviaFrom(node);
    }
}