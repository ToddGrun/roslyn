// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class PropertyDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<PropertyStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        PropertyStatementSyntax propertyDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(propertyDeclaration, spans, options);

        var block = propertyDeclaration.Parent as PropertyBlockSyntax;
        if (block?.EndPropertyStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: propertyDeclaration, autoCollapse: true,
                type: BlockTypes.Member, isCollapsible: true));

            VisualBasicOutliningHelpers.CollectCommentsRegions(block.EndPropertyStatement, spans, options);
        }
    }
}
