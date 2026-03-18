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
internal sealed class ParenthesisBraceCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => Parenthesis.OpenCharacter;
    protected override char ClosingBrace => Parenthesis.CloseCharacter;

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenParenToken);
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.CloseParenToken);
    }

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        if (!IsValidOpeningBraceToken(token) ||
           position != token.SpanStart)
        {
            return false;
        }

        var skippedTriviaNode = token.Parent as SkippedTokensTriviaSyntax;
        if (skippedTriviaNode != null)
        {
            var skippedToken = skippedTriviaNode.ParentTrivia.Token;
            // These checks don't make any sense.  Leaving them in place to avoid breaking something as part of this move.
            if (skippedToken.Kind() != SyntaxKind.CloseParenToken || skippedToken.Parent is not BinaryConditionalExpressionSyntax)
            {
                return false;
            }
        }

        return true;
    }

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);
    }
}
