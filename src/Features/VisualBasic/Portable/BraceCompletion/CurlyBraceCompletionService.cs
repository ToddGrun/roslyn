// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion;

[ExportBraceCompletionService(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CurlyBraceCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => CurlyBrace.OpenCharacter;
    protected override char ClosingBrace => CurlyBrace.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);
    }

    public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
    {
        if (OpeningBrace == brace && InterpolationBraceCompletionService.IsPositionInInterpolationContext(document, openingPosition))
        {
            return false;
        }

        return base.CanProvideBraceCompletion(brace, openingPosition, document, cancellationToken);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenBraceToken);
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.CloseBraceToken);
    }
}
