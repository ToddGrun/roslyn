// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class DisabledTextTriviaStructureProvider : AbstractSyntaxTriviaStructureProvider
{
    public override void CollectBlockSpans(
        SyntaxTrivia trivia,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
        {
            // Don't include trailing line breaks in spanToCollapse
            var nodeSpan = trivia.Span;
            var startPos = nodeSpan.Start;
            var endPos = startPos + trivia.ToString().TrimEnd().Length;

            var span = TextSpan.FromBounds(startPos, endPos);
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpan(
                span: span, hintSpan: span,
                bannerText: VisualBasicOutliningHelpers.Ellipsis, autoCollapse: true,
                type: BlockTypes.PreprocessorRegion,
                isCollapsible: true, isDefaultCollapsed: false));
        }
    }
}
