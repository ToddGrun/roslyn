// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class InterpolatedStringExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<InterpolatedStringExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        InterpolatedStringExpressionSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (node.DollarSignDoubleQuoteToken.IsMissing ||
           node.DoubleQuoteToken.IsMissing)
        {
            return;
        }

        spans.Add(new BlockSpan(
            type: BlockTypes.Expression,
            isCollapsible: true,
            textSpan: node.Span,
            autoCollapse: true,
            isDefaultCollapsed: false));
    }
}
