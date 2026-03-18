// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching;

[ExportBraceMatcher(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class LessThanGreaterThanBraceMatcher()
    : AbstractVisualBasicBraceMatcher(SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken)
{
    protected override bool AllowedForToken(SyntaxToken token)
    {
        // Note: we only need to look for XmlElementStartTag, since brace highlight looks for
        // pairs of matching braces with the same parent.  In the case of other xml nodes, like
        // ElementEndTag, we don't have matching tags.  Instead we have a LessThanSlash token for
        // the </ for example.
        var tok = (SyntaxToken)token;
        return tok.Parent.Kind() != SyntaxKind.XmlElementStartTag;
    }
}
