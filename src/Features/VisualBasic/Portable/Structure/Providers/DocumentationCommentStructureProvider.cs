// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class DocumentationCommentStructureProvider : AbstractSyntaxNodeStructureProvider<DocumentationCommentTriviaSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        DocumentationCommentTriviaSyntax documentationComment,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var firstCommentToken = documentationComment.ChildNodesAndTokens().FirstOrNull();
        var lastCommentToken = documentationComment.ChildNodesAndTokens().LastOrNull();
        if (firstCommentToken == null)
        {
            return;
        }

        // TODO: Need to redo this when DocumentationCommentTrivia.SpanStart points to the start of the exterior trivia.
        var startPos = firstCommentToken.Value.FullSpan.Start;

        // The trailing newline is included in DocumentationCommentTrivia, so we need to strip it.
        var endPos = lastCommentToken.Value.SpanStart + lastCommentToken.Value.ToString().TrimEnd().Length;

        var fullSpan = TextSpan.FromBounds(startPos, endPos);

        var maxBannerLength = options.MaximumBannerLength;
        var bannerText = VisualBasicFileBannerFacts.Instance.GetBannerText(
            documentationComment, maxBannerLength, cancellationToken);

        spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpan(
            fullSpan, fullSpan, bannerText,
            autoCollapse: true, type: BlockTypes.Comment,
            isCollapsible: true, isDefaultCollapsed: false));
    }
}
