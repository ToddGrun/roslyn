// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal abstract class AbstractVisualBasicSignatureHelpProvider : AbstractSignatureHelpProvider
{
    protected static SymbolDisplayPart SynthesizedParameter(string s)
        => new(SymbolDisplayPartKind.ParameterName, null, s);

    protected static SymbolDisplayPart Keyword(SyntaxKind kind)
        => new(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(kind));

    protected static SymbolDisplayPart Punctuation(SyntaxKind kind)
        => new(SymbolDisplayPartKind.Punctuation, null, SyntaxFacts.GetText(kind));

    protected static SymbolDisplayPart Text(string text)
        => new(SymbolDisplayPartKind.Text, null, text);

    protected static SymbolDisplayPart Space()
        => new(SymbolDisplayPartKind.Space, null, " ");

    protected static SymbolDisplayPart NewLine()
        => new(SymbolDisplayPartKind.Space, null, "\r\n");

    protected static IList<SymbolDisplayPart> GetSeparatorParts()
        => [Punctuation(SyntaxKind.CommaToken), Space()];

    protected static SignatureHelpSymbolParameter Convert(IParameterSymbol parameter,
                                      SemanticModel semanticModel,
                                      int position, IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        return new SignatureHelpSymbolParameter(
            parameter.Name,
            parameter.IsOptional,
            parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            parameter.ToMinimalDisplayParts(semanticModel, position));
    }

    protected static void AddExtensionPreamble(ISymbol symbol, IList<SymbolDisplayPart> result)
    {
        if (symbol.GetOriginalUnreducedDefinition().IsExtensionMethod())
        {
            result.Add(Punctuation(SyntaxKind.LessThanToken));
            result.Add(Text(VBFeaturesResources.Extension));
            result.Add(Punctuation(SyntaxKind.GreaterThanToken));
            result.Add(Space());
        }
    }
}
