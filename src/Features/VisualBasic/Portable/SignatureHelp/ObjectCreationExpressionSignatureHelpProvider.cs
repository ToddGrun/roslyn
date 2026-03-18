// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ObjectCreationExpressionSignatureHelpProvider() : AbstractVisualBasicSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    private static SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetObjectCreationExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }

    private static bool TryGetObjectCreationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ObjectCreationExpressionSyntax? expression)
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
            token.Parent.Parent is ObjectCreationExpressionSyntax;

    private static bool IsArgumentListToken(ObjectCreationExpressionSyntax node, SyntaxToken token)
        => node.ArgumentList != null &&
            node.ArgumentList.Span.Contains(token.SpanStart) &&
            token != node.ArgumentList.CloseParenToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetObjectCreationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var objectCreationExpression))
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
        if (type == null)
        {
            return null;
        }

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        var itemsAndSelected = type.TypeKind == TypeKind.Delegate
            ? GetDelegateTypeConstructors(objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, type)
            : GetNormalTypeConstructors(document, objectCreationExpression, semanticModel, structuralTypeDisplayService, type, within, options, cancellationToken);

        return CreateSignatureHelpItems(
            itemsAndSelected.items,
            textSpan,
            GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken),
            itemsAndSelected.selectedItem,
            parameterIndexOverride: -1);
    }

    private static (IList<SignatureHelpItem> items, int? selectedItem) GetDelegateTypeConstructors(ObjectCreationExpressionSyntax objectCreationExpression,
                                                 SemanticModel semanticModel,
                                                 IStructuralTypeDisplayService structuralTypeDisplayService,
                                                 IDocumentationCommentFormattingService documentationCommentFormattingService,
                                                 INamedTypeSymbol delegateType)
    {
        var invokeMethod = delegateType.DelegateInvokeMethod;
        if (invokeMethod == null)
        {
            return (null, null);
        }

        var position = objectCreationExpression.SpanStart;
        var item = CreateItem(
            invokeMethod, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic: false,
            documentationFactory: invokeMethod.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
            separatorParts: GetSeparatorParts(),
            suffixParts: GetDelegateTypePostambleParts(),
            parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position));

        return (Collections.SpecializedCollections.SingletonList(item), 0);
    }

    private static IList<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SignatureHelpSymbolParameter> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
    {
        const string TargetName = "target";

        var parts = new List<SymbolDisplayPart>();

        if (invokeMethod.ReturnsVoid)
        {
            parts.Add(Keyword(SyntaxKind.SubKeyword));
        }
        else
        {
            parts.Add(Keyword(SyntaxKind.FunctionKeyword));
        }

        parts.Add(Space());
        parts.Add(Punctuation(SyntaxKind.OpenParenToken));

        var first = true;
        foreach (var parameter in invokeMethod.Parameters)
        {
            if (!first)
            {
                parts.Add(Punctuation(SyntaxKind.CommaToken));
                parts.Add(Space());
            }

            first = false;
            parts.AddRange(parameter.Type.ToMinimalDisplayParts(semanticModel, position));
        }

        parts.Add(Punctuation(SyntaxKind.CloseParenToken));

        if (!invokeMethod.ReturnsVoid)
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.AsKeyword));
            parts.Add(Space());
            parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        }

        return [new SignatureHelpSymbolParameter(
            TargetName,
            isOptional: false,
            documentationFactory: null,
            displayParts: parts)];
    }

    private static IList<SymbolDisplayPart> GetDelegateTypePostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
