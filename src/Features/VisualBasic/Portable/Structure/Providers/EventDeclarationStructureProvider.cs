// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class EventDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EventStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        EventStatementSyntax eventDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(eventDeclaration, spans, options);

        var block = eventDeclaration.Parent as EventBlockSyntax;
        if (block?.EndEventStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: eventDeclaration, autoCollapse: true,
                type: BlockTypes.Member, isCollapsible: true));

            VisualBasicOutliningHelpers.CollectCommentsRegions(block.EndEventStatement, spans, options);
        }
    }
}
