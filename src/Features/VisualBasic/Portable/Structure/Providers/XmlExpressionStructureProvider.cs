// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class XmlExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<XmlNodeSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        XmlNodeSyntax xmlExpression,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // If this XML expression is inside structured trivia (i.e. an XML doc comment), don't outline.
        if (xmlExpression.HasAncestor<DocumentationCommentTriviaSyntax>())
        {
            return;
        }

        var span = xmlExpression.Span;
        var syntaxTree = xmlExpression.SyntaxTree;
        var line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(span.Start);
        var lineText = line.ToString().Substring(span.Start - line.Start);
        var bannerText = lineText + VisualBasicOutliningHelpers.SpaceEllipsis;

        spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpan(
            span, span, bannerText, autoCollapse: false,
            type: BlockTypes.Expression,
            isCollapsible: true, isDefaultCollapsed: false));
    }
}
