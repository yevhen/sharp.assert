using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert;

public static class SharpAssertRewriter
{
    const string NewLine = "\n";
    const int FirstLineNumber = 1;

    public static string Rewrite(string source, string fileName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);
        var semanticModel = CreateSemanticModel(syntaxTree);
        var absoluteFileName = GetAbsolutePath(fileName);

        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, absoluteFileName, fileName);
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

class SharpAssertSyntaxRewriter(SemanticModel semanticModel, string absoluteFileName, string fileName) : CSharpSyntaxRewriter
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

        var hasAwait = ContainsAwait(node);
        var hasDynamic = ContainsDynamic(node);
        var isBinary = IsBinaryOperation(node);

        HasRewrites = true;

        // Priority: await > dynamic (per PRD section 4.2)
        if (hasAwait)
            return isBinary ? RewriteToAsyncBinary(node) : RewriteToAsync(node);

        if (hasDynamic)
            return isBinary ? RewriteToDynamicBinary(node) : RewriteToDynamic(node);

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
        if (containingType?.Name != "Sharp")
            return false;

        return containingType.ContainingNamespace?.ToDisplayString() == "SharpAssert";
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

    bool ContainsDynamic(InvocationExpressionSyntax node)
    {
        var conditionArgument = node.ArgumentList.Arguments[0];
        var typeInfo = semanticModel.GetTypeInfo(conditionArgument.Expression);

        if (typeInfo.Type?.TypeKind == TypeKind.Dynamic)
            return true;

        // Check for dynamic operations in sub-expressions
        return conditionArgument.DescendantNodes()
            .OfType<ExpressionSyntax>()
            .Any(expr =>
            {
                var exprTypeInfo = semanticModel.GetTypeInfo(expr);
                return exprTypeInfo.Type?.TypeKind == TypeKind.Dynamic;
            });
    }

    static bool IsBinaryOperation(InvocationExpressionSyntax expression) =>
        expression.ArgumentList.Arguments[0].Expression is BinaryExpressionSyntax binaryExpr &&
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

    AwaitExpressionSyntax RewriteToAsyncBinary(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var binaryExpr = (BinaryExpressionSyntax)rewriteData.Expression;

        var leftThunk = CreateAsyncThunk(binaryExpr.Left);
        var rightThunk = CreateAsyncThunk(binaryExpr.Right);
        var binaryOp = GetBinaryOpFromToken(binaryExpr.OperatorToken);

        var newInvocation = CreateAsyncBinaryInvocation(leftThunk, rightThunk, binaryOp, rewriteData);
        var awaitExpr = CreateAwaitExpression(newInvocation);

        return AddLineDirectivesToAwait(awaitExpr, node, rewriteData.LineNumber);
    }

    AwaitExpressionSyntax RewriteToAsync(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var asyncLambda = CreateAsyncLambda(rewriteData.Expression);
        var newInvocation = CreateAsyncInvocation(asyncLambda, rewriteData);
        var awaitExpr = CreateAwaitExpression(newInvocation);

        return AddLineDirectivesToAwait(awaitExpr, node, rewriteData.LineNumber);
    }

    InvocationExpressionSyntax RewriteToDynamic(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var lambda = CreateLambdaExpression(rewriteData.Expression);
        var newInvocation = CreateDynamicInvocation(lambda, rewriteData);
        return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
    }

    InvocationExpressionSyntax RewriteToDynamicBinary(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var binaryExpr = (BinaryExpressionSyntax)rewriteData.Expression;

        var leftThunk = CreateDynamicThunk(binaryExpr.Left);
        var rightThunk = CreateDynamicThunk(binaryExpr.Right);
        var binaryOp = GetBinaryOpFromToken(binaryExpr.OperatorToken);

        var newInvocation = CreateDynamicBinaryInvocation(leftThunk, rightThunk, binaryOp, rewriteData);
        return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
    }

    RewriteData ExtractRewriteData(InvocationExpressionSyntax node)
    {
        var conditionArgument = node.ArgumentList.Arguments[0];
        var expression = conditionArgument.Expression;
        var expressionText = expression.ToString();
        var lineSpan = semanticModel.SyntaxTree.GetLineSpan(node.Span);
        var lineNumber = lineSpan.StartLinePosition.Line + LineNumberOffset;

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
            CreateMessageArgument(data.MessageExpression)
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

    static ArgumentSyntax CreateBooleanLiteralArgument(bool value) =>
        SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression,
                SyntaxFactory.Token(value ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword)));

    static ArgumentSyntax CreateMessageArgument(ExpressionSyntax? messageExpression) =>
        SyntaxFactory.Argument(
            messageExpression ?? SyntaxFactory.LiteralExpression(
                SyntaxKind.NullLiteralExpression,
                SyntaxFactory.Token(SyntaxKind.NullKeyword)));

    InvocationExpressionSyntax AddLineDirectives(InvocationExpressionSyntax invocation, InvocationExpressionSyntax originalNode, int lineNumber) =>
        invocation
            .WithLeadingTrivia(CreateLeadingTrivia(originalNode, lineNumber))
            .WithTrailingTrivia(CreateTrailingTrivia(originalNode));

    AwaitExpressionSyntax AddLineDirectivesToAwait(AwaitExpressionSyntax awaitExpr, InvocationExpressionSyntax originalNode, int lineNumber) =>
        awaitExpr
            .WithLeadingTrivia(CreateLeadingTrivia(originalNode, lineNumber))
            .WithTrailingTrivia(CreateTrailingTrivia(originalNode));

    SyntaxTriviaList CreateLeadingTrivia(InvocationExpressionSyntax originalNode, int lineNumber)
    {
        var trivia = originalNode.GetLeadingTrivia();

        // Lambda expressions need newline before #line to ensure it's at start of line
        if (IsInLambdaBody(originalNode))
            trivia = trivia.Add(SyntaxFactory.EndOfLine(NewLine));

        return trivia
            .Add(SharpAssertRewriter.CreateLineDirective(lineNumber, absoluteFileName))
            .Add(SyntaxFactory.EndOfLine(NewLine));
    }

    static bool IsInLambdaBody(InvocationExpressionSyntax node) =>
        node.Parent is ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax;

    SyntaxTriviaList CreateTrailingTrivia(InvocationExpressionSyntax originalNode) =>
        SyntaxFactory.TriviaList(
            SyntaxFactory.EndOfLine(NewLine),
            SharpAssertRewriter.CreateDefaultLineDirective(),
            SyntaxFactory.EndOfLine(NewLine))
        .AddRange(originalNode.GetTrailingTrivia());

    ParenthesizedLambdaExpressionSyntax CreateAsyncThunk(ExpressionSyntax operand)
    {
        var containsAwait = operand is AwaitExpressionSyntax ||
                           operand.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
        return containsAwait ? CreateAsyncLambda(operand) : WrapInTaskFromResult(operand);
    }

    static ParenthesizedLambdaExpressionSyntax CreateAsyncLambda(ExpressionSyntax operand) =>
        SyntaxFactory.ParenthesizedLambdaExpression()
            .WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(operand);

    static ParenthesizedLambdaExpressionSyntax WrapInTaskFromResult(ExpressionSyntax operand)
    {
        var objectType = CreateNullableObjectType();

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

    static AwaitExpressionSyntax CreateAwaitExpression(InvocationExpressionSyntax invocation) =>
        SyntaxFactory.AwaitExpression(
            SyntaxFactory.Token(
                SyntaxFactory.TriviaList(),
                SyntaxKind.AwaitKeyword,
                SyntaxFactory.TriviaList(SyntaxFactory.Space)),
            invocation);

    static MemberAccessExpressionSyntax CreateSharpInternalMethodAccess(string methodName) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(SharpInternalNamespace),
            SyntaxFactory.IdentifierName(methodName));

    static MemberAccessExpressionSyntax CreateBinaryOpAccess(string binaryOp) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("BinaryOp"),
            SyntaxFactory.IdentifierName(binaryOp));

    static NullableTypeSyntax CreateNullableObjectType() =>
        SyntaxFactory.NullableType(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));

    InvocationExpressionSyntax CreateAsyncBinaryInvocation(
        ParenthesizedLambdaExpressionSyntax leftThunk,
        ParenthesizedLambdaExpressionSyntax rightThunk,
        string binaryOp,
        RewriteData data)
    {
        var targetMethod = CreateSharpInternalMethodAccess("AssertAsyncBinary");
        var binaryOpAccess = CreateBinaryOpAccess(binaryOp);

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

    InvocationExpressionSyntax CreateAsyncInvocation(ParenthesizedLambdaExpressionSyntax asyncLambda, RewriteData data)
    {
        var targetMethod = CreateSharpInternalMethodAccess("AssertAsync");

        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(asyncLambda),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber)
        ]);

        return SyntaxFactory.InvocationExpression(targetMethod)
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    InvocationExpressionSyntax CreateDynamicInvocation(ParenthesizedLambdaExpressionSyntax lambda, RewriteData data)
    {
        var targetMethod = CreateSharpInternalMethodAccess("AssertDynamic");

        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(lambda),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber)
        ]);

        return SyntaxFactory.InvocationExpression(targetMethod)
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    InvocationExpressionSyntax CreateDynamicBinaryInvocation(
        ParenthesizedLambdaExpressionSyntax leftThunk,
        ParenthesizedLambdaExpressionSyntax rightThunk,
        string binaryOp,
        RewriteData data)
    {
        var targetMethod = CreateSharpInternalMethodAccess("AssertDynamicBinary");
        var binaryOpAccess = CreateBinaryOpAccess(binaryOp);

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

    static ParenthesizedLambdaExpressionSyntax CreateDynamicThunk(ExpressionSyntax operand)
    {
        var castToObject = SyntaxFactory.CastExpression(CreateNullableObjectType(), operand);

        return SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithExpressionBody(castToObject);
    }

    record RewriteData(ExpressionSyntax Expression, string ExpressionText, int LineNumber, ExpressionSyntax? MessageExpression);
}