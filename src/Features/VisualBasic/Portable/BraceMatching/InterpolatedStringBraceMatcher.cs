// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching;

[ExportBraceMatcher(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class InterpolatedStringBraceMatcher() : IBraceMatcher
{
    public async Task<BraceMatchingResult?> FindBracesAsync(
        Document document,
        int position,
        BraceMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        if (token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken) &&
           token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression))
        {
            var interpolatedString = (InterpolatedStringExpressionSyntax)token.Parent;

            return new BraceMatchingResult(
                new TextSpan(interpolatedString.DollarSignDoubleQuoteToken.SpanStart, 2),
                new TextSpan(interpolatedString.DoubleQuoteToken.Span.End - 1, 1));
        }

        return null;
    }
}
