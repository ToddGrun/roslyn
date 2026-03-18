// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class CollectionInitializerStructureProvider : AbstractSyntaxNodeStructureProvider<CollectionInitializerSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        CollectionInitializerSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // We don't want to make a span for the "{ ... }" in "From { ... }".  The latter
        // is already handled by ObjectCreationInitializerStructureProvider
        if (node.Parent is not ObjectCollectionInitializerSyntax)
        {
            // We have something like:
            //
            //      New Dictionary(Of int, string) From  {
            //          ...
            //          {
            //              ...
            //          },
            //          ...
            //      }
            //
            //  In this case, we want to collapse the "{ ... }," (including the comma).

            var nextToken = node.CloseBraceToken.GetNextToken();
            var endPos = nextToken.Kind() == SyntaxKind.CommaToken
                ? nextToken.Span.End
                : node.Span.End;

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(node.SpanStart, endPos),
                hintSpan: TextSpan.FromBounds(node.SpanStart, endPos),
                type: BlockTypes.Expression));
        }
    }
}
