// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching;

[ExportBraceMatcher(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicDirectiveTriviaBraceMatcher()
    : AbstractDirectiveTriviaBraceMatcher<DirectiveTriviaSyntax,
         IfDirectiveTriviaSyntax, IfDirectiveTriviaSyntax,
         ElseDirectiveTriviaSyntax, EndIfDirectiveTriviaSyntax,
         RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax>
{
    protected override ImmutableArray<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
    {
        return directive.GetMatchingConditionalDirectives(cancellationToken);
    }

    protected override DirectiveTriviaSyntax GetMatchingDirective(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
    {
        return directive.GetMatchingStartOrEndDirective(cancellationToken);
    }

    internal override TextSpan GetSpanForTagging(DirectiveTriviaSyntax directive)
    {
        var keywordToken = directive.TypeSwitch(
            (IfDirectiveTriviaSyntax context) => context.IfOrElseIfKeyword,
            (ElseDirectiveTriviaSyntax context) => context.ElseKeyword,
            (EndIfDirectiveTriviaSyntax context) => context.IfKeyword,
            (RegionDirectiveTriviaSyntax context) => context.RegionKeyword,
            (EndRegionDirectiveTriviaSyntax context) => context.RegionKeyword);

        return TextSpan.FromBounds(directive.HashToken.SpanStart, keywordToken.Span.End);
    }
}
