// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class ConstructorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<SubNewStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        SubNewStatementSyntax constructorDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(constructorDeclaration, spans, options);

        var block = constructorDeclaration.Parent as ConstructorBlockSyntax;
        if (block?.EndBlockStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: constructorDeclaration, autoCollapse: true,
                type: BlockTypes.Member, isCollapsible: true));
        }
    }
}
