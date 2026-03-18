// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class CompilationUnitStructureProvider : AbstractSyntaxNodeStructureProvider<CompilationUnitSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        CompilationUnitSyntax compilationUnit,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        VisualBasicOutliningHelpers.CollectCommentsRegions(compilationUnit, spans, options);

        if (!compilationUnit.Imports.IsEmpty)
        {
            var startPos = compilationUnit.Imports.First().SpanStart;
            var endPos = compilationUnit.Imports.Last().Span.End;

            var span = TextSpan.FromBounds(startPos, endPos);
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpan(
                span, span, bannerText: "Imports" + VisualBasicOutliningHelpers.SpaceEllipsis,
                autoCollapse: true, type: BlockTypes.Imports, isCollapsible: true,
                isDefaultCollapsed: options.CollapseImportsWhenFirstOpened));
        }

        VisualBasicOutliningHelpers.CollectCommentsRegions(compilationUnit.EndOfFileToken.LeadingTrivia, spans);
    }
}
