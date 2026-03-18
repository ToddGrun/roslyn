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
internal sealed class LessAndGreaterThanCompletionService()
    : AbstractVisualBasicBraceCompletionService
{
    protected override char OpeningBrace => LessAndGreaterThan.OpenCharacter;
    protected override char ClosingBrace => LessAndGreaterThan.CloseCharacter;

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.LessThanToken);
    }

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.LessThanGreaterThanToken);
    }

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        if (!token.CheckParent<AttributeListSyntax>(n => n.LessThanToken == token) &&
           !token.CheckParent<XmlNamespaceImportsClauseSyntax>(n => n.LessThanToken == token) &&
           !token.CheckParent<XmlBracketedNameSyntax>(n => n.LessThanToken == token))
        {
            return false;
        }

        return true;
    }

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);
    }
}
