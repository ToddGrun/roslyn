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
internal sealed class InterpolationBraceCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => CurlyBrace.OpenCharacter;
    protected override char ClosingBrace => CurlyBrace.CloseCharacter;

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        return IsValidOpeningBraceToken(token);
    }

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeWithValidClosingToken(context);
    }

    public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
    {
        return OpeningBrace == brace && IsPositionInInterpolationContext(document, openingPosition);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) ||
               (token.IsKind(SyntaxKind.CloseBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation));
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.CloseBraceToken);
    }

    public static bool IsPositionInInterpolationContext(ParsedDocument document, int position)
    {
        if (position == 0)
        {
            return false;
        }

        // First, check to see if the character to the left of the position is an open curly.
        // If it is, we shouldn't complete because the user may be trying to escape a curly.
        // E.g. they are trying to type $"{{"
        if (CouldEscapePreviousOpenBrace('{', position, document.Text))
        {
            return false;
        }

        // Next, check to see if the token we're typing is part of an existing interpolated string.
        var token = document.Root.FindTokenOnRightOfPosition(position);

        if (!token.Span.IntersectsWith(position))
        {
            return false;
        }

        return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) ||
           (token.IsKind(SyntaxKind.CloseBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation));
    }
}
