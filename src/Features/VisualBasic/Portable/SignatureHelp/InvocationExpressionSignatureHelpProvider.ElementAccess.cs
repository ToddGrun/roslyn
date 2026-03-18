// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal sealed partial class InvocationExpressionSignatureHelpProvider
{
    private static IEnumerable<SignatureHelpItem> GetElementAccessItems(ExpressionSyntax leftExpression,
                                           SemanticModel semanticModel,
                                           IStructuralTypeDisplayService structuralTypeDisplayService,
                                           IDocumentationCommentFormattingService documentationCommentFormattingService,
                                           ISymbol within,
                                           IList<IPropertySymbol> defaultProperties,
                                           CancellationToken cancellationToken)
    {
        ITypeSymbol? throughType = null;
        if (leftExpression != null)
        {
            throughType = semanticModel.GetTypeInfo(leftExpression, cancellationToken).Type;
        }

        var accessibleDefaultProperties = defaultProperties.Where(m => m.IsAccessibleWithin(within, throughType: throughType)).ToList();
        if (accessibleDefaultProperties.Count == 0)
        {
            return SpecializedCollections.EmptyEnumerable<SignatureHelpItem>();
        }

        return accessibleDefaultProperties.Select(
            s => ConvertIndexer(s, leftExpression.SpanStart, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));
    }

    private static SignatureHelpItem ConvertIndexer(IPropertySymbol indexer,
                                    int position,
                                    SemanticModel semanticModel,
                                    IStructuralTypeDisplayService structuralTypeDisplayService,
                                    IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var item = CreateItem(
            indexer, semanticModel, position,
            structuralTypeDisplayService,
            indexer.IsParams(),
            indexer.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetIndexerPreambleParts(indexer, semanticModel, position),
            GetSeparatorParts(),
            GetIndexerPostambleParts(indexer, semanticModel, position),
            indexer.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
        return item;
    }

    private static IList<SymbolDisplayPart> GetIndexerPreambleParts(IPropertySymbol symbol, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        result.AddRange(symbol.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SymbolDisplayPart> GetIndexerPostambleParts(IPropertySymbol symbol,
                                              SemanticModel semanticModel,
                                              int position)
    {
        var parts = new List<SymbolDisplayPart>();
        parts.Add(Punctuation(SyntaxKind.CloseParenToken));

        var property = (IPropertySymbol)symbol;

        parts.Add(Space());
        parts.Add(Keyword(SyntaxKind.AsKeyword));
        parts.Add(Space());
        parts.AddRange(property.Type.ToMinimalDisplayParts(semanticModel, position));

        return parts;
    }
}
