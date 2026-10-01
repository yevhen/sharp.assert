using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpAssert;

public static class SharpAssertRewriter
{
    const string NewLine = "\n";
    const int FirstLineNumber = 1;

    public static string Rewrite(string source, string fileName) => Rewrite(source, fileName, null);

    public static string Rewrite(string source, string fileName, string? globalUsings)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);
        var semanticModel = CreateSemanticModel(syntaxTree, globalUsings);
        var absoluteFileName = GetAbsolutePath(fileName);

        var rewriter = new SharpAssertSyntaxRewriter(semanticModel, absoluteFileName, fileName);
        var rewrittenRoot = rewriter.Visit(syntaxTree.GetRoot());

        if (!rewriter.HasRewrites)
            return source;

        return AddFileLineDirective(rewrittenRoot, absoluteFileName);
    }

    static SemanticModel CreateSemanticModel(SyntaxTree syntaxTree, string? globalUsings)
    {
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Sharp).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create("RewriterAnalysis")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        if (globalUsings is not null)
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(globalUsings));

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
                SyntaxFactory.EndOfLine(NewLine))
            .AddRange(rewrittenRoot.GetLeadingTrivia()));

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
    const string AssertInternalMethodName = "AssertValue";
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

        // Priority: await > dynamic (per PRD section 4.2)
        if (hasAwait)
        {
            if (IsBooleanAssertion(node.ArgumentList.Arguments[0].Expression))
            {
                HasRewrites = true;
                return isBinary ? RewriteToAsyncBinary(node) : RewriteToAsync(node);
            }

            return base.VisitInvocationExpression(node);
        }

        if (hasDynamic)
        {
            HasRewrites = true;
            return isBinary ? RewriteToDynamicBinary(node) : RewriteToDynamic(node);
        }

        HasRewrites = true;
        var condition = node.ArgumentList.Arguments[0].Expression;
        if (condition is InvocationExpressionSyntax enumerableCall && IsEnumerableCall(enumerableCall, "All"))
            return RewriteToAll(node, enumerableCall);

        if (condition is InvocationExpressionSyntax anyCall && IsEnumerableCall(anyCall, "Any"))
            return RewriteToAny(node, anyCall);

        if (IsExpectationAssertion(condition))
            return RewriteToExpectation(node);

        if (ContainsExceptionResult(condition))
            return condition is BinaryExpressionSyntax inlineComparison && IsBinaryOperation(node) && CanUseFastComparison(inlineComparison)
                ? RewriteToComparison(node, inlineComparison)
                : RewriteToBoolean(node, condition);

        if (condition.DescendantNodesAndSelf().OfType<IsPatternExpressionSyntax>().Any())
        {
            var unwrapped = condition;
            while (unwrapped is ParenthesizedExpressionSyntax group)
                unwrapped = group.Expression;

            return unwrapped is IsPatternExpressionSyntax pattern && CanCaptureType(pattern.Expression) &&
                   !pattern.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>().Any()
                ? RewriteToPattern(node, pattern)
                : RewriteToBoolean(node, condition);
        }

        if (condition is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member, ArgumentList.Arguments.Count: 1 } call &&
            (CanUseFastArraySequenceEqual(call, member) || CanUseFastMethodCall(call, member)))
            return RewriteToMethodCall(node, call, member);

        if (CanUseSimpleBoolean(condition) && IsBooleanAssertion(condition))
            return RewriteToBoolean(node, condition);

        if (condition is BinaryExpressionSyntax comparison &&
            IsBinaryOperation(node) && CanUseFastComparison(comparison))
            return RewriteToComparison(node, comparison);

        if (IsBooleanAssertion(condition) && ContainsLocalFunction(condition))
            return RewriteToBoolean(node, condition);

        if (IsBooleanAssertion(condition) && TryCapture(condition, out var captured, out var count))
            return RewriteToCaptured(node, captured, count);

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

        if (methodSymbol.Parameters is not [{ Type: var firstParameterType }, ..])
            return false;

        if (firstParameterType.Name != "AssertValue" ||
            firstParameterType.ContainingNamespace.ToDisplayString() != "SharpAssert")
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

    bool IsBooleanAssertion(ExpressionSyntax expression)
    {
        if (expression is AwaitExpressionSyntax awaitExpression)
            return IsAwaitingBoolean(awaitExpression);

        var typeInfo = semanticModel.GetTypeInfo(expression);
        return typeInfo.Type?.SpecialType == SpecialType.System_Boolean;
    }

    bool ContainsLocalFunction(ExpressionSyntax condition) =>
        condition.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(call => semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction });

    static bool CanUseSimpleBoolean(ExpressionSyntax condition)
    {
        if (condition is IdentifierNameSyntax or MemberAccessExpressionSyntax)
            return true;

        if (condition is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation)
            return false;

        var name = invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => null
        };
        return name is not ("Contains" or "Any" or "All" or "SequenceEqual");
    }

    bool IsEnumerableCall(InvocationExpressionSyntax call, string name) =>
        semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
        method.Name == name &&
        method.ContainingType.ToDisplayString() == "System.Linq.Enumerable" &&
        IsBooleanAssertion(call);

    bool IsExpectationAssertion(ExpressionSyntax expression)
    {
        var expectationType = semanticModel.Compilation.GetTypeByMetadataName("SharpAssert.Expectation");
        var type = semanticModel.GetTypeInfo(expression).Type;

        for (; type is not null; type = (type as INamedTypeSymbol)?.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, expectationType))
                return true;
        }

        return false;
    }

    bool IsAwaitingBoolean(AwaitExpressionSyntax awaitExpression)
    {
        var awaitedType = semanticModel.GetTypeInfo(awaitExpression.Expression).Type;
        if (awaitedType is not INamedTypeSymbol { IsGenericType: true, TypeArguments: [var resultType] } namedType)
            return false;

        if (namedType.Name is not ("Task" or "ValueTask"))
            return false;

        return resultType.SpecialType == SpecialType.System_Boolean;
    }

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

    InvocationExpressionSyntax RewriteToBoolean(InvocationExpressionSyntax node, ExpressionSyntax condition)
    {
        var data = ExtractRewriteData(node);
        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(EagerCondition(GuardExceptionReads(condition, data))),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertBoolean"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    InvocationExpressionSyntax RewriteToPattern(InvocationExpressionSyntax node, IsPatternExpressionSyntax pattern)
    {
        var data = ExtractRewriteData(node);
        var name = "patternValue";
        for (var suffix = 1; pattern.DescendantTokens().Any(token => token.ValueText == name); suffix++)
            name = $"patternValue{suffix}";

        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(name));
        var condition = pattern.WithExpression(SyntaxFactory.IdentifierName(name))
            .WithIsKeyword(SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.Space),
                SyntaxKind.IsKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
        var matches = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter)), condition);
        ExpressionSyntax messageFactory = data.MessageExpression is null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : CreateLambdaExpression(data.MessageExpression);
        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(pattern.Expression),
            SyntaxFactory.Argument(matches),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            SyntaxFactory.Argument(messageFactory)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertPattern"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    bool CanUseFastArraySequenceEqual(InvocationExpressionSyntax call, MemberAccessExpressionSyntax member) =>
        member.Name.Identifier.ValueText == "SequenceEqual" &&
        IsBooleanAssertion(call) &&
        semanticModel.GetTypeInfo(member.Expression).Type is IArrayTypeSymbol &&
        semanticModel.GetTypeInfo(call.ArgumentList.Arguments[0].Expression).Type is IArrayTypeSymbol;

    bool CanUseFastMethodCall(InvocationExpressionSyntax call, MemberAccessExpressionSyntax member)
    {
        var name = member.Name.Identifier.ValueText;
        if (name is "Any" or "All" or "SequenceEqual")
            return false;

        var receiverType = semanticModel.GetTypeInfo(member.Expression).Type;
        var argumentType = semanticModel.GetTypeInfo(call.ArgumentList.Arguments[0].Expression).Type;
        return IsBooleanAssertion(call) &&
               receiverType is { IsRefLikeType: false } &&
               argumentType is { IsRefLikeType: false };
    }

    InvocationExpressionSyntax RewriteToMethodCall(InvocationExpressionSyntax node, InvocationExpressionSyntax call, MemberAccessExpressionSyntax member)
    {
        var data = ExtractRewriteData(node);
        var receiver = SyntaxFactory.IdentifierName("receiver");
        var argument = SyntaxFactory.IdentifierName("argument");
        var predicate = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("receiver")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("argument"))
            ])),
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, member.Name),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument)))));

        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(member.Expression),
            SyntaxFactory.Argument(call.ArgumentList.Arguments[0].Expression),
            SyntaxFactory.Argument(predicate),
            SyntaxFactory.Argument(CreateLambdaExpression(GenerateExprNodeSyntax(data.Expression))),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression)
        ]);
        var methodName = member.Name.Identifier.ValueText switch
        {
            "Contains" => "AssertContains",
            "SequenceEqual" => "AssertSequenceEqual",
            _ => "AssertMethodCall"
        };
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess(methodName))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    bool CanUseFastComparison(BinaryExpressionSyntax comparison)
    {
        if (semanticModel.GetConstantValue(comparison).HasValue)
            return false;

        var leftType = semanticModel.GetTypeInfo(comparison.Left).Type;
        var rightType = semanticModel.GetTypeInfo(comparison.Right).Type;
        if (leftType is null || rightType is null)
            return false;

        if (comparison.OperatorToken.IsKind(SyntaxKind.LessThanToken) ||
            comparison.OperatorToken.IsKind(SyntaxKind.LessThanEqualsToken) ||
            comparison.OperatorToken.IsKind(SyntaxKind.GreaterThanToken) ||
            comparison.OperatorToken.IsKind(SyntaxKind.GreaterThanEqualsToken))
            return IsNumeric(leftType) && IsNumeric(rightType);

        return IsSimpleComparable(leftType) && IsSimpleComparable(rightType);
    }

    static bool IsNumeric(ITypeSymbol type) => type.SpecialType is
        SpecialType.System_Byte or SpecialType.System_SByte or
        SpecialType.System_Int16 or SpecialType.System_UInt16 or
        SpecialType.System_Int32 or SpecialType.System_UInt32 or
        SpecialType.System_Int64 or SpecialType.System_UInt64 or
        SpecialType.System_Single or SpecialType.System_Double or
        SpecialType.System_Decimal or SpecialType.System_Char;

    static bool IsSimpleComparable(ITypeSymbol type) => IsNumeric(type) || type.SpecialType is
        SpecialType.System_String or SpecialType.System_Boolean;

    InvocationExpressionSyntax RewriteToComparison(InvocationExpressionSyntax node, BinaryExpressionSyntax comparison)
    {
        var data = ExtractRewriteData(node);
        var left = SyntaxFactory.IdentifierName("left");
        var right = SyntaxFactory.IdentifierName("right");
        var predicate = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("left")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("right"))
            ])),
            SyntaxFactory.BinaryExpression(comparison.Kind(), left, right));

        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(GuardExceptionReads(comparison.Left, data)),
            SyntaxFactory.Argument(GuardExceptionReads(comparison.Right, data)),
            SyntaxFactory.Argument(predicate),
            SyntaxFactory.Argument(CreateBinaryOpAccess(GetBinaryOpFromToken(comparison.OperatorToken))),
            SyntaxFactory.Argument(CreateLambdaExpression(GenerateExprNodeSyntax(data.Expression))),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertComparison"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    InvocationExpressionSyntax RewriteToExpectation(InvocationExpressionSyntax node)
    {
        var data = ExtractRewriteData(node);
        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(data.Expression),
            SyntaxFactory.Argument(GenerateExprNodeSyntax(data.Expression)),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertExpectationValue"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    InvocationExpressionSyntax RewriteToAll(InvocationExpressionSyntax node, InvocationExpressionSyntax call)
    {
        var data = ExtractRewriteData(node);
        var isStatic = call.ArgumentList.Arguments.Count == 2;
        var source = isStatic
            ? call.ArgumentList.Arguments[0].Expression
            : ((MemberAccessExpressionSyntax)call.Expression).Expression;
        var predicate = call.ArgumentList.Arguments[isStatic ? 1 : 0].Expression;
        ExpressionSyntax messageFactory = data.MessageExpression is null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : CreateLambdaExpression(data.MessageExpression);

        var arguments = SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(source),
            SyntaxFactory.Argument(predicate),
            CreateStringLiteralArgument(predicate.ToString()),
            SyntaxFactory.Argument(CreateLambdaExpression(GenerateExprNodeSyntax(data.Expression))),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            SyntaxFactory.Argument(messageFactory)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertAll"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    InvocationExpressionSyntax RewriteToAny(InvocationExpressionSyntax node, InvocationExpressionSyntax call)
    {
        var data = ExtractRewriteData(node);
        var method = (IMethodSymbol)semanticModel.GetSymbolInfo(call).Symbol!;
        var isStatic = method.ReducedFrom is null;
        var source = isStatic
            ? call.ArgumentList.Arguments[0].Expression
            : ((MemberAccessExpressionSyntax)call.Expression).Expression;
        var predicateIndex = isStatic ? 1 : 0;
        var arguments = new List<ArgumentSyntax> { SyntaxFactory.Argument(source) };
        if (call.ArgumentList.Arguments.Count > predicateIndex)
        {
            var predicate = call.ArgumentList.Arguments[predicateIndex].Expression;
            arguments.Add(SyntaxFactory.Argument(predicate));
            arguments.Add(CreateStringLiteralArgument(predicate.ToString()));
        }

        ExpressionSyntax messageFactory = data.MessageExpression is null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : CreateLambdaExpression(data.MessageExpression);
        arguments.Add(SyntaxFactory.Argument(CreateLambdaExpression(GenerateExprNodeSyntax(data.Expression))));
        arguments.Add(CreateStringLiteralArgument(fileName));
        arguments.Add(CreateNumericLiteralArgument(data.LineNumber));
        arguments.Add(SyntaxFactory.Argument(messageFactory));

        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertAny"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    InvocationExpressionSyntax RewriteToCaptured(InvocationExpressionSyntax node, ExpressionSyntax captured, int count)
    {
        var data = ExtractRewriteData(node);
        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("session"));
        var parameters = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
        var evaluate = SyntaxFactory.ParenthesizedLambdaExpression(parameters, captured);
        var expression = CreateLambdaExpression(captured);
        var describe = SyntaxFactory.ParenthesizedLambdaExpression(parameters, expression);
        ExpressionSyntax messageFactory = data.MessageExpression is null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : CreateLambdaExpression(data.MessageExpression);

        var arguments = SyntaxFactory.SeparatedList([
            CreateNumericLiteralArgument(count),
            SyntaxFactory.Argument(evaluate),
            SyntaxFactory.Argument(describe),
            SyntaxFactory.Argument(CreateLambdaExpression(GenerateExprNodeSyntax(data.Expression))),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            SyntaxFactory.Argument(messageFactory)
        ]);
        var invocation = SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess("AssertCaptured"))
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        return AddLineDirectives(invocation, node, data.LineNumber);
    }

    bool TryCapture(ExpressionSyntax expression, out ExpressionSyntax captured, out int count)
    {
        if (semanticModel.GetConstantValue(expression).HasValue)
        {
            captured = null!;
            count = 0;
            return false;
        }

        count = 0;
        captured = Capture(expression, ref count)!;
        return captured is not null;
    }

    ExpressionSyntax? Capture(ExpressionSyntax expression, ref int count)
    {
        if (!CanCaptureType(expression))
            return null;

        ExpressionSyntax body = expression;
        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            var inner = Capture(parenthesized.Expression, ref count);
            if (inner is null)
                return null;
            body = parenthesized.WithExpression(inner);
        }
        else if (expression is BinaryExpressionSyntax binary && IsBinaryComparisonExpression(binary))
        {
            if (binary.OperatorToken.IsKind(SyntaxKind.AmpersandToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.BarToken))
                return null;

            if (binary.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.LessThanToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.LessThanEqualsToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.GreaterThanToken) ||
                binary.OperatorToken.IsKind(SyntaxKind.GreaterThanEqualsToken))
            {
                if (!CanUseFastComparison(binary))
                    return null;
            }

            var left = Capture(binary.Left, ref count);
            var right = Capture(binary.Right, ref count);
            if (left is null || right is null)
                return null;
            body = binary.IsKind(SyntaxKind.LogicalAndExpression)
                ? SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseAndExpression, left, right).WithTriviaFrom(binary)
                : binary.WithLeft(left).WithRight(right);
        }
        else if (expression is BinaryExpressionSyntax && semanticModel.GetTypeInfo(expression).Type?.SpecialType == SpecialType.System_Boolean)
            return null;
        else if (expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
        {
            var operand = Capture(unary.Operand, ref count);
            if (operand is null)
                return null;
            body = unary.WithOperand(operand);
        }
        else if (expression is InvocationExpressionSyntax call)
        {
            var method = semanticModel.GetSymbolInfo(call).Symbol as IMethodSymbol;
            if (method is null || method.Parameters.Any(parameter => parameter.RefKind != RefKind.None || parameter.Type.IsRefLikeType))
                return null;

            var target = call.Expression;
            if (target is MemberAccessExpressionSyntax member &&
                semanticModel.GetSymbolInfo(member.Expression).Symbol is not INamedTypeSymbol &&
                semanticModel.GetTypeInfo(member.Expression).Type is { IsValueType: false } receiverType &&
                receiverType.TypeKind != TypeKind.Error)
            {
                var receiver = Capture(member.Expression, ref count);
                if (receiver is null)
                    return null;
                target = member.WithExpression(receiver);
            }
            else if (target is MemberAccessExpressionSyntax memberWithValueReceiver &&
                     semanticModel.GetTypeInfo(memberWithValueReceiver.Expression).Type is { IsValueType: true })
                return null;

            var arguments = new List<ArgumentSyntax>();
            foreach (var argument in call.ArgumentList.Arguments)
            {
                if (argument.Expression is AnonymousFunctionExpressionSyntax)
                {
                    arguments.Add(argument);
                    continue;
                }

                var capturedArgument = Capture(argument.Expression, ref count);
                if (capturedArgument is null)
                    return null;
                arguments.Add(argument.WithExpression(capturedArgument));
            }

            body = call.WithExpression(target)
                .WithArgumentList(call.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(arguments)));
        }

        var index = count++;
        var record = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression("session.Record"),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                CreateNumericLiteralArgument(index),
                SyntaxFactory.Argument(body.WithoutLeadingTrivia().WithoutTrailingTrivia())
            ])));
        return record.WithTriviaFrom(expression);
    }

    static ExpressionSyntax EagerCondition(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax group => group.WithExpression(EagerCondition(group.Expression)),
        PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.LogicalNotExpression) =>
            unary.WithOperand(EagerCondition(unary.Operand)),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) =>
            SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseAndExpression,
                SyntaxFactory.ParenthesizedExpression(EagerCondition(binary.Left)),
                SyntaxFactory.ParenthesizedExpression(EagerCondition(binary.Right))).WithTriviaFrom(binary),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) =>
            binary.WithLeft(EagerCondition(binary.Left)).WithRight(EagerCondition(binary.Right)),
        _ => expression
    };

    bool CanCaptureType(ExpressionSyntax expression)
    {
        var type = semanticModel.GetTypeInfo(expression).Type;
        return type is not null && type.TypeKind is not (TypeKind.Error or TypeKind.Dynamic or TypeKind.Pointer) &&
               type.SpecialType != SpecialType.System_Void && !type.IsRefLikeType;
    }

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

        var leftThunk = CreateAsyncThunk(GuardExceptionReads(binaryExpr.Left, rewriteData));
        var rightThunk = CreateAsyncThunk(GuardExceptionReads(binaryExpr.Right, rewriteData));
        var binaryOp = GetBinaryOpFromToken(binaryExpr.OperatorToken);

        var newInvocation = CreateAsyncBinaryInvocation(leftThunk, rightThunk, binaryOp, rewriteData);
        var awaitExpr = CreateAwaitExpression(newInvocation);

        return AddLineDirectivesToAwait(awaitExpr, node, rewriteData.LineNumber);
    }

    AwaitExpressionSyntax RewriteToAsync(InvocationExpressionSyntax node)
    {
        var rewriteData = ExtractRewriteData(node);
        var asyncLambda = CreateAsyncLambda(EagerCondition(GuardExceptionReads(rewriteData.Expression, rewriteData)));
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
            SyntaxFactory.IdentifierName(AssertInternalMethodName));

    SeparatedSyntaxList<ArgumentSyntax> CreateInvocationArguments(ParenthesizedLambdaExpressionSyntax lambdaExpression, RewriteData data)
    {
        var exprNodeSyntax = GenerateExprNodeSyntax(data.Expression);

        return SyntaxFactory.SeparatedList([
            SyntaxFactory.Argument(lambdaExpression),
            SyntaxFactory.Argument(exprNodeSyntax),
            CreateStringLiteralArgument(data.ExpressionText),
            CreateStringLiteralArgument(fileName),
            CreateNumericLiteralArgument(data.LineNumber),
            CreateMessageArgument(data.MessageExpression)
        ]);
    }

    static ExpressionSyntax GenerateExprNodeSyntax(ExpressionSyntax expression)
    {
        var unwrapped = expression;
        while (unwrapped is ParenthesizedExpressionSyntax parenthesized)
            unwrapped = parenthesized.Expression;

        if (unwrapped is BinaryExpressionSyntax binaryExpr && IsBinaryComparisonExpression(binaryExpr))
            return CreateExprNodeObjectCreation(binaryExpr, expression);

        if (unwrapped is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            return CreateUnaryExprNodeObjectCreation(unaryExpr, expression);

        if (unwrapped is InvocationExpressionSyntax invocationExpr)
            return CreateMethodCallExprNodeObjectCreation(invocationExpr, expression);

        // Generate simple ExprNode for all other expressions (variables, arithmetic, etc.)
        return CreateSimpleExprNode(expression);
    }

    static bool IsBinaryComparisonExpression(BinaryExpressionSyntax binaryExpr) =>
        binaryExpr.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.LessThanToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.LessThanEqualsToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.GreaterThanToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.GreaterThanEqualsToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.BarBarToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.AmpersandToken) ||
        binaryExpr.OperatorToken.IsKind(SyntaxKind.BarToken);

    static ObjectCreationExpressionSyntax CreateExprNodeObjectCreation(BinaryExpressionSyntax binaryExpr, ExpressionSyntax originalExpression)
    {
        var text = originalExpression.ToString();
        var leftNode = GenerateExprNodeSyntax(binaryExpr.Left);
        var rightNode = GenerateExprNodeSyntax(binaryExpr.Right);

        var leftNameColon = SyntaxFactory.NameColon(
            SyntaxFactory.IdentifierName("Left"),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));
        var rightNameColon = SyntaxFactory.NameColon(
            SyntaxFactory.IdentifierName("Right"),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));

        var arguments = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(text))),
                SyntaxFactory.Argument(leftNode)
                    .WithNameColon(leftNameColon),
                SyntaxFactory.Argument(rightNode)
                    .WithNameColon(rightNameColon)
            ]));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                CreateExprNodeTypeName(),
                arguments,
                default);
    }

    static ObjectCreationExpressionSyntax CreateUnaryExprNodeObjectCreation(PrefixUnaryExpressionSyntax unaryExpr, ExpressionSyntax originalExpression)
    {
        var text = originalExpression.ToString();
        var operandNode = GenerateExprNodeSyntax(unaryExpr.Operand);

        var nameColon = SyntaxFactory.NameColon(
            SyntaxFactory.IdentifierName("Operand"),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));

        var arguments = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(text))),
                SyntaxFactory.Argument(operandNode)
                    .WithNameColon(nameColon)
            ]));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                CreateExprNodeTypeName(),
                arguments,
                default);
    }

    static ObjectCreationExpressionSyntax CreateMethodCallExprNodeObjectCreation(InvocationExpressionSyntax invocationExpr, ExpressionSyntax originalExpression)
    {
        var text = originalExpression.ToString();
        var receiverNode = invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess
            ? GenerateExprNodeSyntax(memberAccess.Expression)
            : null;

        var argumentNodes = invocationExpr.ArgumentList.Arguments
            .Select(arg => GenerateExprNodeSyntax(arg.Expression))
            .ToArray();

        var argumentsArray = CreateExprNodeArray(argumentNodes);

        var receiverNameColon = SyntaxFactory.NameColon(
            SyntaxFactory.IdentifierName("Left"),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));

        var nameColon = SyntaxFactory.NameColon(
            SyntaxFactory.IdentifierName("Arguments"),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));

        var list = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(text)))
        };

        if (receiverNode is not null)
        {
            list.Add(
                SyntaxFactory.Argument(receiverNode)
                    .WithNameColon(receiverNameColon));
        }

        list.Add(
            SyntaxFactory.Argument(argumentsArray)
                .WithNameColon(nameColon));

        var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(list));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                CreateExprNodeTypeName(),
                arguments,
                default);
    }

    static ExpressionSyntax CreateExprNodeArray(ExpressionSyntax[] nodes)
    {
        if (nodes.Length == 0)
        {
            var emptyArray = SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(
                    CreateExprNodeTypeName(),
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)))))));

            return emptyArray.WithNewKeyword(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space));
        }

        var array = SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(
                CreateExprNodeTypeName(),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))),
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(nodes)));

        return array.WithNewKeyword(
            SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space));
    }

    static ObjectCreationExpressionSyntax CreateSimpleExprNode(ExpressionSyntax expression)
    {
        var text = expression.ToString();

        var arguments = SyntaxFactory.ArgumentList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(text)))));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                CreateExprNodeTypeName(),
                arguments,
                default);
    }

    static QualifiedNameSyntax CreateExprNodeTypeName() =>
        SyntaxFactory.QualifiedName(
            SyntaxFactory.AliasQualifiedName(
                SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                SyntaxFactory.IdentifierName("SharpAssert")),
            SyntaxFactory.IdentifierName("ExprNode"));

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

        return trivia
            .Add(SyntaxFactory.EndOfLine(NewLine))
            .Add(SharpAssertRewriter.CreateLineDirective(lineNumber, absoluteFileName))
            .Add(SyntaxFactory.EndOfLine(NewLine));
    }

    SyntaxTriviaList CreateTrailingTrivia(InvocationExpressionSyntax originalNode) =>
        SyntaxFactory.TriviaList(
            SyntaxFactory.EndOfLine(NewLine),
            SharpAssertRewriter.CreateLineDirective(
                semanticModel.SyntaxTree.GetLineSpan(originalNode.Span).EndLinePosition.Line + LineNumberOffset,
                absoluteFileName),
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
            SyntaxFactory.IdentifierName("global::SharpAssert.BinaryOp"),
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

    bool ContainsExceptionResult(ExpressionSyntax expression) =>
        expression.DescendantNodesAndSelf().OfType<ExpressionSyntax>()
            .Any(each => IsExceptionResultType(semanticModel.GetTypeInfo(each).Type));

    static bool IsExceptionResultType(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "ExceptionResult", ContainingType.Name: "Sharp" } &&
        type.ContainingNamespace.ToDisplayString() == "SharpAssert";

    ExpressionSyntax GuardExceptionReads(ExpressionSyntax expression, RewriteData data) =>
        (ExpressionSyntax)new ExceptionReadGuard(semanticModel, data, fileName).Visit(expression)!;

    sealed class ExceptionReadGuard(SemanticModel semanticModel, RewriteData data, string fileName) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is AnonymousFunctionExpressionSyntax)
                return node;

            if (node is not ExpressionSyntax expression ||
                !IsExceptionResultType(semanticModel.GetTypeInfo(expression).Type) ||
                semanticModel.GetSymbolInfo(expression).Symbol is INamedTypeSymbol ||
                expression is ParenthesizedExpressionSyntax ||
                expression is InvocationExpressionSyntax call && IsThrowsCall(call))
                return base.Visit(node);

            var value = (ExpressionSyntax)base.Visit(node)!;
            return CreateInvocation(SyntaxFactory.IdentifierName("RequireException"), value.WithoutTrivia())
                .WithTriviaFrom(expression);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!IsThrowsCall(node))
                return base.VisitInvocationExpression(node);

            var name = node.Expression is GenericNameSyntax generic
                ? generic
                : (GenericNameSyntax)((MemberAccessExpressionSyntax)node.Expression).Name;
            var methodName = name.Identifier.ValueText == "Throws" ? "CaptureException" : "CaptureExceptionAsync";
            var action = (ExpressionSyntax)Visit(node.ArgumentList.Arguments[0].Expression)!;
            return CreateInvocation(name.WithIdentifier(SyntaxFactory.Identifier(methodName)), action).WithTriviaFrom(node);
        }

        bool IsThrowsCall(InvocationExpressionSyntax call) =>
            semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol
            {
                Name: "Throws" or "ThrowsAsync",
                ContainingType.Name: "Sharp"
            } method && method.ContainingNamespace.ToDisplayString() == "SharpAssert";

        InvocationExpressionSyntax CreateInvocation(SimpleNameSyntax method, ExpressionSyntax value)
        {
            ExpressionSyntax messageFactory = data.MessageExpression is null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : CreateLambdaExpression(data.MessageExpression);
            return SyntaxFactory.InvocationExpression(CreateSharpInternalMethodAccess(method.Identifier.ValueText).WithName(method),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(value),
                    CreateStringLiteralArgument(data.ExpressionText),
                    CreateStringLiteralArgument(fileName),
                    CreateNumericLiteralArgument(data.LineNumber),
                    SyntaxFactory.Argument(messageFactory)
                ])));
        }
    }

    record RewriteData(ExpressionSyntax Expression, string ExpressionText, int LineNumber, ExpressionSyntax? MessageExpression);
}
