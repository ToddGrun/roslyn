// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal class RegionDirectiveStructureProvider : AbstractSyntaxNodeStructureProvider<RegionDirectiveTriviaSyntax>
{
    private static string GetBannerText(RegionDirectiveTriviaSyntax regionDirective)
    {
        var text = regionDirective.Name.ToString().Trim('"');

        if (text.Length == 0)
        {
            return regionDirective.HashToken.ToString() + regionDirective.RegionKeyword.ToString();
        }

        return text;
    }

    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        RegionDirectiveTriviaSyntax regionDirective,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var matchingDirective = regionDirective.GetMatchingStartOrEndDirective(cancellationToken);
        if (matchingDirective != null)
        {
            // Always auto-collapse regions for Metadata As Source. These generated files only have one region at the
            // top of the file, which has content like the following:
            //
            //   #Region "Assembly System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            //   ' C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll
            //   #End Region
            //
            // For other files, auto-collapse regions based on the user option.
            var autoCollapse = options.IsMetadataAsSource || options.CollapseRegionsWhenCollapsingToDefinitions;

            var span = TextSpan.FromBounds(regionDirective.SpanStart, matchingDirective.Span.End);
            spans.AddIfNotNull(VisualBasicOutliningHelpers.CreateBlockSpan(
                span, span,
                GetBannerText(regionDirective),
                autoCollapse: autoCollapse,
                isDefaultCollapsed: options.CollapseRegionsWhenFirstOpened,
                type: BlockTypes.PreprocessorRegion,
                isCollapsible: true));
        }
    }
}
