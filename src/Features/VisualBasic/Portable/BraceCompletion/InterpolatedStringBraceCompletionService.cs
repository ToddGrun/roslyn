// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion;

[ExportBraceCompletionService(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class InterpolatedStringBraceCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => DoubleQuote.OpenCharacter;
    protected override char ClosingBrace => DoubleQuote.CloseCharacter;

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        return IsValidOpeningBraceToken(token) && token.Span.End - 1 == position;
    }

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeWithValidClosingToken(context);
    }

    public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
    {
        return OpeningBrace == brace && IsPositionInInterpolatedStringContext(document, openingPosition);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken);
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.DoubleQuoteToken);
    }

    public static bool IsPositionInInterpolatedStringContext(ParsedDocument document, int position)
    {
        if (position == 0)
        {
            return false;
        }

        // Position can be in an interpolated string if the preceding character is a $
        return document.Text[position - 1] == '$';
    }
}
