// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.VisualBasic), Shared]
[ExtensionOrder(After = nameof(KeywordCompletionProvider))]
internal sealed class AwaitCompletionProvider : AbstractAwaitCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AwaitCompletionProvider()
        : base(VisualBasicSyntaxFacts.Instance)
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    public override ImmutableHashSet<char> TriggerCharacters => CommonTriggerChars;

    protected override int GetAsyncKeywordInsertionPosition(SyntaxNode declaration)
    {
        switch (declaration.Kind())
        {
            case SyntaxKind.FunctionBlock:
            case SyntaxKind.SubBlock:
                return ((DeclarationStatementSyntax)declaration).GetMemberKeywordToken().SpanStart;
            case SyntaxKind.MultiLineFunctionLambdaExpression:
            case SyntaxKind.MultiLineSubLambdaExpression:
            case SyntaxKind.SingleLineFunctionLambdaExpression:
            case SyntaxKind.SingleLineSubLambdaExpression:
                return ((LambdaExpressionSyntax)declaration).SubOrFunctionHeader.SubOrFunctionKeyword.SpanStart;
        }

        throw ExceptionUtilities.Unreachable;
    }

    protected override Task<TextChange?> GetReturnTypeChangeAsync(Solution solution, SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        // Todo: Add support if desired.
        return SpecializedTasks.Default<TextChange?>();
    }

    protected override SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken targetToken, int position)
        => targetToken.GetAncestor<SyntaxNode>(node => node.IsAsyncSupportedFunctionSyntax());

    protected override ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken)
    {
        var memberAccessExpression = (potentialAwaitableExpression as MemberAccessExpressionSyntax)?.Expression;
        if (memberAccessExpression == null)
        {
            return null;
        }

        var symbol = semanticModel.GetSymbolInfo(memberAccessExpression.WalkDownParentheses(), cancellationToken).Symbol;
        return symbol is ITypeSymbol ? null : semanticModel.GetTypeInfo(memberAccessExpression, cancellationToken).Type;
    }

    protected override SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
        if (!dotToken.HasValue)
        {
            return null;
        }

        var memberAccess = dotToken.Value.Parent as MemberAccessExpressionSyntax;
        if (memberAccess == null)
        {
            return null;
        }

        if (memberAccess.Expression.GetParentConditionalAccessExpression() != null)
        {
            return null;
        }

        return memberAccess;
    }

    protected override SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        var dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position);
        if (!dotToken.IsKind(SyntaxKind.DotToken))
        {
            return null;
        }

        if (dotToken.GetPreviousToken().IsKind(SyntaxKind.IntegerLiteralToken, SyntaxKind.FloatingLiteralToken, SyntaxKind.DecimalLiteralToken, SyntaxKind.DateLiteralToken))
        {
            return null;
        }

        return dotToken;
    }
}
