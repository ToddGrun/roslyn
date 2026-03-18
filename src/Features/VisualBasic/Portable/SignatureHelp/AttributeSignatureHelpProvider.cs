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
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("AttributeSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class AttributeSignatureHelpProvider() : AbstractVisualBasicSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    private bool TryGetAttributeExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out AttributeSyntax? attribute)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out attribute))
        {
            return false;
        }

        return attribute.ArgumentList != null;
    }

    private bool IsTriggerToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) &&
            token.Parent is ArgumentListSyntax &&
            token.Parent.Parent is AttributeSyntax;

    private static bool IsArgumentListToken(AttributeSyntax node, SyntaxToken token)
        => node.ArgumentList != null &&
            node.ArgumentList.Span.Contains(token.SpanStart) &&
            token != node.ArgumentList.CloseParenToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetAttributeExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var attribute))
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
        if (attributeType == null)
        {
            return null;
        }

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var accessibleConstructors = attributeType.InstanceConstructors
                                                   .WhereAsArray(c => c.IsAccessibleWithin(within))
                                                   .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                                   .Sort(semanticModel, attribute.SpanStart);

        if (!accessibleConstructors.Any())
        {
            return null;
        }

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(attribute.ArgumentList);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        var symbolInfo = semanticModel.GetSymbolInfo(attribute, cancellationToken);
        var selectedItem = TryGetSelectedIndex(accessibleConstructors, symbolInfo.Symbol);

        return CreateSignatureHelpItems(accessibleConstructors.Select(
            c => Convert(c, within, attribute, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem, parameterIndexOverride: -1);
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetAttributeExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }

    private static SignatureHelpItem Convert(IMethodSymbol constructor,
                                       ISymbol within,
                                       AttributeSyntax attribute,
                                       SemanticModel semanticModel,
                                       IStructuralTypeDisplayService structuralTypeDisplayService,
                                       IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = attribute.SpanStart;
        var namedParameters = constructor.ContainingType.GetAttributeNamedParameters(semanticModel.Compilation, within)
                                                         .OrderBy(s => s.Name)
                                                         .ToList();

        var isVariadic =
            constructor.Parameters.Length > 0 && constructor.Parameters.Last().IsParams && namedParameters.Count == 0;

        var item = CreateItem(
            constructor, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic,
            constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetPreambleParts(constructor, semanticModel, position),
            GetSeparatorParts(),
            GetPostambleParts(),
            GetParameters(constructor, semanticModel, position, namedParameters, documentationCommentFormattingService));
        return item;
    }

    private static IList<SignatureHelpSymbolParameter> GetParameters(IMethodSymbol constructor,
                                   SemanticModel semanticModel,
                                   int position,
                                   List<ISymbol> namedParameters,
                                   IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var result = new List<SignatureHelpSymbolParameter>();

        foreach (var parameter in constructor.Parameters)
        {
            result.Add(Convert(parameter, semanticModel, position, documentationCommentFormattingService));
        }

        for (var i = 0; i < namedParameters.Count; i++)
        {
            var namedParameter = namedParameters[i];

            var type = namedParameter is IFieldSymbol fieldSymbol
                           ? fieldSymbol.Type
                           : ((IPropertySymbol)namedParameter).Type;

            var displayParts = new List<SymbolDisplayPart>();

            displayParts.Add(new SymbolDisplayPart(
                namedParameter is IFieldSymbol ? SymbolDisplayPartKind.FieldName : SymbolDisplayPartKind.PropertyName,
                namedParameter, namedParameter.Name.ToIdentifierToken().ToString()));
            displayParts.Add(Punctuation(SyntaxKind.ColonEqualsToken));
            displayParts.AddRange(type.ToMinimalDisplayParts(semanticModel, position));

            result.Add(new SignatureHelpSymbolParameter(
                namedParameter.Name,
                isOptional: true,
                documentationFactory: namedParameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                displayParts: displayParts,
                prefixDisplayParts: GetParameterPrefixDisplayParts(i)));
        }

        return result;
    }

    private static List<SymbolDisplayPart>? GetParameterPrefixDisplayParts(int i)
    {
        if (i == 0)
        {
            return
            [
                new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, FeaturesResources.Properties),
                Punctuation(SyntaxKind.ColonToken),
                Space()
            ];
        }

        return null;
    }

    private static IList<SymbolDisplayPart> GetPreambleParts(IMethodSymbol method, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SymbolDisplayPart> GetPostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
