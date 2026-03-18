// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class ObjectCreationInitializerStructureProvider : AbstractSyntaxNodeStructureProvider<ObjectCreationInitializerSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        ObjectCreationInitializerSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // ObjectCreationInitializerSyntax is either "With { ... }" or "From { ... }"
        // Parent is something like
        //
        //      New Dictionary(Of int, string) From {
        //          ...
        //      }
        //
        // The collapsed textspan should be from the   )   to the   }
        //
        // However, the hint span should be the entire object creation.
        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: TextSpan.FromBounds(previousToken.Span.End, node.Span.End),
            hintSpan: node.Parent.Span,
            type: BlockTypes.Expression));
    }
}
