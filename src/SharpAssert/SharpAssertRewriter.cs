using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert;

public static class SharpAssertRewriter
{
    const string NewLine = "\n";
    const int FirstLineNumber = 1;

    public static string Rewrite(string source, string fileName, bool usePowerAssert = false, bool usePowerAssertForUnsupported = true)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);
        var semanticModel = CreateSemanticModel(syntaxTree);
        var absoluteFileName = GetAbsolutePath(fileName);
        
        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, absoluteFileName, fileName, usePowerAssert, usePowerAssertForUnsupported);
        var rewrittenRoot = rewriter.Visit(syntaxTree.GetRoot());

        if (!rewriter.HasRewrites)
            return source;

        return AddFileLineDirective(rewrittenRoot, absoluteFileName);
    }

    static SemanticModel CreateSemanticModel(SyntaxTree syntaxTree)
    {
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Sharp).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create("RewriterAnalysis")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        return compilation.GetSemanticModel(syntaxTree);
    }

    static string GetAbsolutePath(string fileName) =>
        Path.IsPathRooted(fileName) ? fileName : Path.GetFullPath(fileName);

    static string AddFileLineDirective(SyntaxNode rewrittenRoot, string absoluteFileName)
    {
        var nullableRestoreDirective = SyntaxFactory.PreprocessingMessage("#nullable restore");
        var lineDirective = CreateLineDirective(FirstLineNumber, absoluteFileName);
        var rewrittenWithDirectives = rewrittenRoot.WithLeadingTrivia(
            SyntaxFactory.TriviaList(
                nullableRestoreDirective,
                SyntaxFactory.EndOfLine(NewLine),
                lineDirective,
                SyntaxFactory.EndOfLine(NewLine)));

        return rewrittenWithDirectives.ToFullString();
    }

    public static SyntaxTrivia CreateLineDirective(int lineNumber, string filePath) =>
        SyntaxFactory.PreprocessingMessage($"#line {lineNumber} \"{EscapeFilePath(filePath)}\"");

    public static SyntaxTrivia CreateDefaultLineDirective() =>
        SyntaxFactory.PreprocessingMessage("#line default");

    public static string EscapeFilePath(string filePath) =>
        filePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

internal class SharpAssertSyntaxRewriter(SemanticModel semanticModel, string absoluteFileName, string fileName, bool usePowerAssert, bool usePowerAssertForUnsupported) : CSharpSyntaxRewriter
{
    const string SharpInternalNamespace = "global::SharpAssert.SharpInternal";
    const string AssertMethodName = "Assert";
    const string NewLine = "\n";
    const int LineNumberOffset = 1;

    public bool HasRewrites { get; private set; }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (!IsSharpAssertCall(node))
            return base.VisitInvocationExpression(node);

        var containsAwait = ContainsAwait(node);
        if (containsAwait)
        {
            var conditionArgument = node.ArgumentList.Arguments[0];
            var isBinaryOperation = IsBinaryOperation(conditionArgument.Expression);
            
            if (isBinaryOperation)
            {
                HasRewrites = true;
                return RewriteToAsyncBinary(node);
            }
            else
            {
                // General await case - skip rewriting for now (will be handled by AssertAsync in future)
                return base.VisitInvocationExpression(node);
            }
        }

        HasRewrites = true;
        return RewriteToLambda(node);
    }

    bool IsSharpAssertCall(InvocationExpressionSyntax node)
    {
        if (node.Expression is not IdentifierNameSyntax identifier ||
            identifier.Identifier.ValueText != AssertMethodName)
            return false;

        var methodSymbol = GetMethodSymbol(node);
        if (methodSymbol == null)
            return false;

        var containingType = methodSymbol.ContainingType;
        return containingType?.Name == "Sharp" &&
               containingType.ContainingNamespace?.ToDisplayString() == "SharpAssert";
    }

    IMethodSymbol? GetMethodSymbol(InvocationExpressionSyntax node)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(node);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            return methodSymbol;

        return symbolInfo.CandidateSymbols is [IMethodSymbol candidateMethod] ? candidateMethod : null;
    }

    static bool ContainsAwait(InvocationExpressionSyntax node) =>
        node.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .Any();

    static bool IsBinaryOperation(ExpressionSyntax expression) =>
        expression is BinaryExpressionSyntax binaryExpr &&
        (binaryExpr.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) ||
         binaryExpr.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken) ||
         binaryExpr.OperatorToken.IsKind(SyntaxKind.LessThanToken) ||
         binaryExpr.OperatorToken.IsKind(SyntaxKind.LessThanEqualsToken) ||
         binaryExpr.OperatorToken.IsKind(SyntaxKind.GreaterThanToken) ||
         binaryExpr.OperatorToken.IsKind(SyntaxKind.GreaterThanEqualsToken));

    InvocationExpressionSyntax RewriteToLambda(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var lambdaExpression = CreateLambdaExpression(rewriteData.Expression);
        var newInvocation = CreateSharpInternalInvocation(lambdaExpression, rewriteData);
        return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
    }

    InvocationExpressionSyntax RewriteToAsyncBinary(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var binaryExpr = (BinaryExpressionSyntax)rewriteData.Expression;
        
        var leftThunk = CreateAsyncThunk(binaryExpr.Left);
        var rightThunk = CreateAsyncThunk(binaryExpr.Right);
        var binaryOp = GetBinaryOpFromToken(binaryExpr.OperatorToken);
        
        var newInvocation = CreateAsyncBinaryInvocation(leftThunk, rightThunk, binaryOp, rewriteData);
        return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
    }

    RewriteData ExtractRewriteData(InvocationExpressionSyntax node)
    {
        var conditionArgument = node.ArgumentList.Arguments[0];
        var expression = conditionArgument.Expression;
        var expressionText = expression.ToString();
        var lineNumber = semanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + LineNumberOffset;
        
        var messageExpression = node.ArgumentList.Arguments.Count > 1
            ? node.ArgumentList.Arguments[1].Expression
            : null;
        
        return new RewriteData(expression, expressionText, lineNumber, messageExpression);
    }

    static ParenthesizedLambdaExpressionSyntax CreateLambdaExpression(ExpressionSyntax expression) =>
        SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(expression);

    InvocationExpressionSyntax CreateSharpInternalInvocation(ParenthesizedLambdaExpressionSyntax lambdaExpression, RewriteData data)
    {
        var targetMethod = CreateTargetMethodAccess();
        var arguments = CreateInvocationArguments(lambdaExpression, data);
        
        return SyntaxFactory.InvocationExpression(targetMethod)
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    static MemberAccessExpressionSyntax CreateTargetMethodAccess() =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(SharpInternalNamespace),
            SyntaxFactory.IdentifierName(AssertMethodName));

    SeparatedSyntaxList<ArgumentSyntax> CreateInvocationArguments(ParenthesizedLambdaExpressionSyntax lambdaExpression, RewriteData data) =>
        SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(lambdaExpression),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression),
            CreateBooleanLiteralArgument(usePowerAssert),
            CreateBooleanLiteralArgument(usePowerAssertForUnsupported)
        ]);

    static ArgumentSyntax CreateStringLiteralArgument(string value) =>
        SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(value)));

    static ArgumentSyntax CreateNumericLiteralArgument(int value) =>
        SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(value)));

    static ArgumentSyntax CreateMessageArgument(ExpressionSyntax? messageExpression) =>
        SyntaxFactory.Argument(
            messageExpression ?? SyntaxFactory.LiteralExpression(
                SyntaxKind.NullLiteralExpression,
                SyntaxFactory.Token(SyntaxKind.NullKeyword)));

    static ArgumentSyntax CreateBooleanLiteralArgument(bool value) =>
        SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression,
                SyntaxFactory.Token(value ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword)));

    InvocationExpressionSyntax AddLineDirectives(InvocationExpressionSyntax invocation, InvocationExpressionSyntax originalNode, int lineNumber) =>
        invocation
            .WithLeadingTrivia(CreateLeadingTrivia(originalNode, lineNumber))
            .WithTrailingTrivia(CreateTrailingTrivia(originalNode));

    SyntaxTriviaList CreateLeadingTrivia(InvocationExpressionSyntax originalNode, int lineNumber) =>
        originalNode.GetLeadingTrivia()
            .Add(SharpAssertRewriter.CreateLineDirective(lineNumber, absoluteFileName))
            .Add(SyntaxFactory.EndOfLine(NewLine));

    SyntaxTriviaList CreateTrailingTrivia(InvocationExpressionSyntax originalNode) =>
        SyntaxFactory.TriviaList(
            SyntaxFactory.EndOfLine(NewLine),
            SharpAssertRewriter.CreateDefaultLineDirective(),
            SyntaxFactory.EndOfLine(NewLine))
        .AddRange(originalNode.GetTrailingTrivia());

    ParenthesizedLambdaExpressionSyntax CreateAsyncThunk(ExpressionSyntax operand)
    {
        var containsAwait = operand.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
        
        if (containsAwait)
        {
            // Create async () => operand
            return SyntaxFactory.ParenthesizedLambdaExpression()
                .WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithExpressionBody(operand);
        }
        else
        {
            // Wrap sync operand: () => Task.FromResult<object?>(operand)
            var objectType = SyntaxFactory.NullableType(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));
                
            var taskFromResult = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Task"),
                    SyntaxFactory.GenericName("FromResult")
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(objectType)))))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(operand))));
            
            return SyntaxFactory.ParenthesizedLambdaExpression()
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithExpressionBody(taskFromResult);
        }
    }

    static string GetBinaryOpFromToken(SyntaxToken operatorToken) => operatorToken.Kind() switch
    {
        SyntaxKind.EqualsEqualsToken => "Eq",
        SyntaxKind.ExclamationEqualsToken => "Ne", 
        SyntaxKind.LessThanToken => "Lt",
        SyntaxKind.LessThanEqualsToken => "Le",
        SyntaxKind.GreaterThanToken => "Gt",
        SyntaxKind.GreaterThanEqualsToken => "Ge",
        _ => "Eq" // fallback
    };

    InvocationExpressionSyntax CreateAsyncBinaryInvocation(
        ParenthesizedLambdaExpressionSyntax leftThunk, 
        ParenthesizedLambdaExpressionSyntax rightThunk, 
        string binaryOp,
        RewriteData data)
    {
        var targetMethod = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(SharpInternalNamespace),
            SyntaxFactory.IdentifierName("AssertAsyncBinary"));
        
        var binaryOpAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("BinaryOp"),
            SyntaxFactory.IdentifierName(binaryOp));
        
        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(leftThunk),
            SyntaxFactory.Argument(rightThunk),
            SyntaxFactory.Argument(binaryOpAccess),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber)
        ]);
        
        return SyntaxFactory.InvocationExpression(targetMethod)
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    record RewriteData(ExpressionSyntax Expression, string ExpressionText, int LineNumber, ExpressionSyntax? MessageExpression);
}