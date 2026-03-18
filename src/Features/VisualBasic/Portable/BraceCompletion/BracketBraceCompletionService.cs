// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion;

[ExportBraceCompletionService(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BracketBraceCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => Bracket.OpenCharacter;
    protected override char ClosingBrace => Bracket.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenBraceToken);
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        // Identifiers in VB can be bracketed, e.g. Dim [Dim].  The closing brace token in this case is an identifier token.
        return token.IsKind(SyntaxKind.CloseBraceToken, SyntaxKind.IdentifierToken);
    }

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        if (position == token.SpanStart &&
           token.Kind() == SyntaxKind.BadToken &&
           token.ToString() == Bracket.OpenCharacter.ToString())
        {
            return !IsBracketInCData(token);
        }

        if (position < token.SpanStart)
        {
            return false;
        }

        foreach (var trivia in token.TrailingTrivia)
        {
            var span = trivia.Span;

            if (span.End < position)
            {
                return false;
            }
            else if (!span.IntersectsWith(position) ||
                   !trivia.HasStructure)
            {
                continue;
            }

            if (trivia.GetStructure() is SkippedTokensTriviaSyntax)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBracketInCData(SyntaxToken token)
    {
        var skippedToken = token.Parent as SkippedTokensTriviaSyntax;
        if (skippedToken == null)
        {
            return false;
        }

        return skippedToken.ParentTrivia.Token.Kind() == SyntaxKind.GreaterThanToken;
    }
}
