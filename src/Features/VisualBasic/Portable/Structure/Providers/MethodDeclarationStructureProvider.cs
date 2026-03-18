// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class MethodDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<MethodStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        MethodStatementSyntax methodDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(methodDeclaration, spans, options);

        var block = methodDeclaration.Parent as MethodBlockSyntax;
        if (block?.EndBlockStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: methodDeclaration, autoCollapse: true,
                type: BlockTypes.Member, isCollapsible: true));
        }
    }
}
