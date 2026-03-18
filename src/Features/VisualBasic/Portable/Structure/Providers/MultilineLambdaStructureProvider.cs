// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class MultilineLambdaStructureProvider : AbstractSyntaxNodeStructureProvider<MultiLineLambdaExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        MultiLineLambdaExpressionSyntax lambdaExpression,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (!lambdaExpression.EndSubOrFunctionStatement.IsMissing)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                lambdaExpression, bannerNode: lambdaExpression.SubOrFunctionHeader, autoCollapse: false,
                type: BlockTypes.Expression, isCollapsible: true));
        }
    }
}
