// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class NamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<NamespaceStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        NamespaceStatementSyntax namespaceDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(namespaceDeclaration, spans, options);

        var block = namespaceDeclaration.Parent as NamespaceBlockSyntax;
        if (block?.EndNamespaceStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: namespaceDeclaration, autoCollapse: false,
                type: BlockTypes.Namespace, isCollapsible: true));

            VisualBasicOutliningHelpers.CollectCommentsRegions(block.EndNamespaceStatement, spans, options);
        }
    }
}
