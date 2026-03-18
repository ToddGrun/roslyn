// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("InvocationExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class InvocationExpressionSignatureHelpProvider() : AbstractOrdinaryMethodSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    private static SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetInvocationExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }

    private static bool TryGetInvocationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out InvocationExpressionSyntax? expression)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
        {
            return false;
        }

        return expression.ArgumentList != null;
    }

    private static bool IsTriggerToken(SyntaxToken token)
        => (token.Kind() == SyntaxKind.OpenParenToken || token.Kind() == SyntaxKind.CommaToken) &&
            token.Parent is ArgumentListSyntax &&
            token.Parent.Parent is InvocationExpressionSyntax;

    private static bool IsArgumentListToken(InvocationExpressionSyntax node, SyntaxToken token)
        => node.ArgumentList != null &&
            node.ArgumentList.Span.Contains(token.SpanStart) &&
            token != node.ArgumentList.CloseParenToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetInvocationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var invocationExpression))
        {
            return null;
        }

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(invocationExpression, cancellationToken).ConfigureAwait(false);
        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var targetExpression = invocationExpression.Expression == null && invocationExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression)
            ? ((ConditionalAccessExpressionSyntax)invocationExpression.Parent).Expression
            : invocationExpression.Expression;

        // get the regular signature help items
        var memberGroup = semanticModel.GetMemberGroup(targetExpression, cancellationToken)
                                        .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation);

        // try to bind to the actual method
        var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
        var matchedMethodSymbol = symbolInfo.Symbol as IMethodSymbol;

        // if the symbol could be bound, replace that item in the symbol list
        if (matchedMethodSymbol != null && matchedMethodSymbol.IsGenericMethod)
        {
            memberGroup = memberGroup.SelectAsArray(m => Equals(matchedMethodSymbol.OriginalDefinition, m) ? matchedMethodSymbol : m);
        }

        var enclosingSymbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
        if (enclosingSymbol.IsConstructor())
        {
            memberGroup = memberGroup.WhereAsArray(m => !m.Equals(enclosingSymbol));
        }

        memberGroup = memberGroup.Sort(semanticModel, invocationExpression.SpanStart);

        var typeInfo = semanticModel.GetTypeInfo(targetExpression, cancellationToken);
        var expressionType = typeInfo.Type ?? typeInfo.ConvertedType;
        var defaultProperties =
            expressionType == null
               ? SpecializedCollections.EmptyList<IPropertySymbol>()
               : semanticModel.LookupSymbols(position, expressionType, includeReducedExtensionMethods: true)
                             .OfType<IPropertySymbol>()
                             .ToImmutableArrayOrEmpty()
                             .WhereAsArray(p => p.IsIndexer)
                             .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                             .Sort(semanticModel, invocationExpression.SpanStart);

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

        var items = new List<SignatureHelpItem>();
        var accessibleMembers = ImmutableArray<ISymbol>.Empty;
        if (memberGroup.Length > 0)
        {
            accessibleMembers = GetAccessibleMembers(invocationExpression, semanticModel, within, memberGroup, cancellationToken);
            items.AddRange(GetMemberGroupItems(accessibleMembers, document, invocationExpression, semanticModel));
        }

        if (expressionType.IsDelegateType())
        {
            items.AddRange(GetDelegateInvokeItems(invocationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, (INamedTypeSymbol)expressionType, cancellationToken));
        }

        if (defaultProperties.Count > 0)
        {
            items.AddRange(GetElementAccessItems(targetExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, within, defaultProperties, cancellationToken));
        }

        var textSpan = GetSignatureHelpSpan(invocationExpression.ArgumentList);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        var selectedItem = TryGetSelectedIndex(accessibleMembers, symbolInfo.Symbol);
        return CreateSignatureHelpItems(
            items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken),
            selectedItem, parameterIndexOverride: -1);
    }

    private static ImmutableArray<ISymbol> GetAccessibleMembers(InvocationExpressionSyntax invocationExpression,
                                         SemanticModel semanticModel,
                                         ISymbol within,
                                         IEnumerable<ISymbol> memberGroup,
                                         CancellationToken cancellationToken)
    {
        ITypeSymbol? throughType = null;
        var expression = (invocationExpression.Expression as MemberAccessExpressionSyntax).GetExpressionOfMemberAccessExpression();

        // if it is via a base expression "MyBase.", we know the "throughType" is the base class but
        // we need to be able to tell between "New Base().M()" and "MyBase.M()".
        // currently, Access check methods do not differentiate between them.
        // so handle "MyBase." primary-expression here by nulling out "throughType"
        if (expression != null && expression is not MyBaseExpressionSyntax)
        {
            throughType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        }

        if (invocationExpression.Expression is SimpleNameSyntax &&
           invocationExpression.IsInStaticContext())
        {
            memberGroup = memberGroup.Where(m => m.IsStatic);
        }

        return memberGroup.Where(m => m.IsAccessibleWithin(within, throughType)).ToImmutableArray();
    }

    private static IEnumerable<SignatureHelpItem> GetMemberGroupItems(ImmutableArray<ISymbol> accessibleMembers,
                                         Document document,
                                         InvocationExpressionSyntax invocationExpression,
                                         SemanticModel semanticModel)
    {
        if (accessibleMembers.Length == 0)
        {
            return SpecializedCollections.EmptyEnumerable<SignatureHelpItem>();
        }

        return accessibleMembers.Select(
            s => ConvertMemberGroupMember(document, s, invocationExpression.SpanStart, semanticModel));
    }

    private static IEnumerable<SignatureHelpItem> GetDelegateInvokeItems(InvocationExpressionSyntax invocationExpression,
                                            SemanticModel semanticModel,
                                            IStructuralTypeDisplayService structuralTypeDisplayService,
                                            IDocumentationCommentFormattingService documentationCommentFormattingService,
                                            INamedTypeSymbol delegateType,
                                            CancellationToken cancellationToken)
    {
        var invokeMethod = delegateType.DelegateInvokeMethod;
        if (invokeMethod == null)
        {
            return SpecializedCollections.EmptyEnumerable<SignatureHelpItem>();
        }

        var position = invocationExpression.SpanStart;
        var item = CreateItem(
            invokeMethod, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic: invokeMethod.IsParams(),
            documentationFactory: null,
            prefixParts: GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
            separatorParts: GetSeparatorParts(),
            suffixParts: GetDelegateInvokePostambleParts(invokeMethod, semanticModel, position),
            parameters: GetDelegateInvokeParameters(invokeMethod, semanticModel, position, documentationCommentFormattingService, cancellationToken));
        return SpecializedCollections.SingletonEnumerable(item);
    }

    private static IList<SymbolDisplayPart> GetDelegateInvokePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
    {
        var displayParts = new List<SymbolDisplayPart>();

        if (invokeMethod.ContainingType.IsAnonymousType)
        {
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, invokeMethod, invokeMethod.Name));
        }
        else
        {
            displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        }

        displayParts.Add(Punctuation(SyntaxKind.OpenParenToken));
        return displayParts;
    }

    private static IList<SignatureHelpSymbolParameter> GetDelegateInvokeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService documentationCommentFormattingService, CancellationToken cancellationToken)
    {
        var parameters = new List<SignatureHelpSymbolParameter>();
        foreach (var parameter in invokeMethod.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            parameters.Add(new SignatureHelpSymbolParameter(
                parameter.Name,
                isOptional: false,
                documentationFactory: parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                displayParts: parameter.ToMinimalDisplayParts(semanticModel, position)));
        }

        return parameters;
    }

    private static IList<SymbolDisplayPart> GetDelegateInvokePostambleParts(IMethodSymbol invokeMethod,
                                                     SemanticModel semanticModel,
                                                     int position)
    {
        var parts = new List<SymbolDisplayPart>();

        parts.Add(Punctuation(SyntaxKind.CloseParenToken));

        if (!invokeMethod.ReturnsVoid)
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.AsKeyword));
            parts.Add(Space());
            parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        }

        return parts;
    }
}
