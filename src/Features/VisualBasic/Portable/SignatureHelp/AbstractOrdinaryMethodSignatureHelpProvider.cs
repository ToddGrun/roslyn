// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal abstract class AbstractOrdinaryMethodSignatureHelpProvider : AbstractVisualBasicSignatureHelpProvider
{
    protected static SignatureHelpItem ConvertMemberGroupMember(Document document,
                                                ISymbol member,
                                                int position,
                                                SemanticModel semanticModel)
    {
        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

        return CreateItem(
            member, semanticModel, position,
            structuralTypeDisplayService,
            member.IsParams(),
            c => member.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c),
            GetMemberGroupPreambleParts(member, semanticModel, position),
            GetSeparatorParts(),
            GetMemberGroupPostambleParts(member, semanticModel, position),
            member.GetParameters().Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
    }

    private static IList<SymbolDisplayPart> GetMemberGroupPreambleParts(ISymbol symbol, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();

        AddExtensionPreamble(symbol, result);

        result.AddRange(symbol.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.DotToken));

        var format = MinimallyQualifiedWithoutParametersFormat;
        format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeContainingType);
        format = format.RemoveKindOptions(SymbolDisplayKindOptions.IncludeMemberKeyword);
        format = format.WithMiscellaneousOptions(format.MiscellaneousOptions & ~SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        result.AddRange(symbol.ToMinimalDisplayParts(semanticModel, position, format));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SymbolDisplayPart> GetMemberGroupPostambleParts(ISymbol symbol,
                                                  SemanticModel semanticModel,
                                                  int position)
    {
        var parts = new List<SymbolDisplayPart>();
        parts.Add(Punctuation(SyntaxKind.CloseParenToken));

        if (symbol is IMethodSymbol method)
        {
            if (!method.ReturnsVoid)
            {
                parts.Add(Space());
                parts.Add(Keyword(SyntaxKind.AsKeyword));
                parts.Add(Space());
                parts.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position));
            }
        }
        else if (symbol is IPropertySymbol property)
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.AsKeyword));
            parts.Add(Space());
            parts.AddRange(property.Type.ToMinimalDisplayParts(semanticModel, position));
        }

        return parts;
    }
}
