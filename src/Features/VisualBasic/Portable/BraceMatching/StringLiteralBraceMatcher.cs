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

namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching;

[ExportBraceMatcher(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class StringLiteralBraceMatcher() : IBraceMatcher
{
    public async Task<BraceMatchingResult?> FindBracesAsync(Document document,
                                     int position,
                                     BraceMatchingOptions options,
                                     CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        if (position == token.SpanStart || position == token.Span.End - 1)
        {
            if (token.Kind() == SyntaxKind.StringLiteralToken && !token.ContainsDiagnostics)
            {
                return new BraceMatchingResult(
                    new TextSpan(token.SpanStart, 1),
                    new TextSpan(token.Span.End - 1, 1));
            }
        }

        return null;
    }
}
