// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class TypeDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<TypeStatementSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        TypeStatementSyntax typeDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(typeDeclaration, spans, options);

        var block = typeDeclaration.Parent as TypeBlockSyntax;
        if (block?.EndBlockStatement.IsMissing == false)
        {
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpanFromBlock(
                block, bannerNode: typeDeclaration, autoCollapse: false,
                type: BlockTypes.Type, isCollapsible: true));

            VisualBasicOutliningHelpers.CollectCommentsRegions(block.EndBlockStatement, spans, options);
        }
    }
}
