// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class ForBlockStructureProvider : AbstractSyntaxNodeStructureProvider<ForBlockSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        ForBlockSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
            node, node.ForOrForEachStatement, autoCollapse: false,
            type: BlockTypes.Loop, isCollapsible: true));
    }
}
