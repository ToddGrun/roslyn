// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal sealed partial class ObjectCreationExpressionSignatureHelpProvider
{
    private static (IList<SignatureHelpItem> items, int? selectedItem) GetNormalTypeConstructors(
        Document document,
        ObjectCreationExpressionSyntax objectCreationExpression,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        INamedTypeSymbol normalType,
        ISymbol within,
        MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var accessibleConstructors = normalType.InstanceConstructors
                                                .WhereAsArray(c => c.IsAccessibleWithin(within))
                                                .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                                .Sort(semanticModel, objectCreationExpression.SpanStart);

        if (!accessibleConstructors.Any())
        {
            return default;
        }

        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

        var items = accessibleConstructors.Select(
            c => ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList();

        var currentConstructor = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken);
        var selectedItem = TryGetSelectedIndex(accessibleConstructors, currentConstructor.Symbol);

        return (items, selectedItem);
    }

    private static SignatureHelpItem ConvertNormalTypeConstructor(IMethodSymbol constructor, ObjectCreationExpressionSyntax objectCreationExpression, SemanticModel semanticModel,
                                                  IStructuralTypeDisplayService structuralTypeDisplayService,
                                                  IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = objectCreationExpression.SpanStart;
        var item = CreateItem(
            constructor, semanticModel, position,
            structuralTypeDisplayService,
            constructor.IsParams(),
            constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetNormalTypePreambleParts(constructor, semanticModel, position), GetSeparatorParts(),
            GetNormalTypePostambleParts(),
            constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
        return item;
    }

    private static IList<SymbolDisplayPart> GetNormalTypePreambleParts(IMethodSymbol method, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SymbolDisplayPart> GetNormalTypePostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
