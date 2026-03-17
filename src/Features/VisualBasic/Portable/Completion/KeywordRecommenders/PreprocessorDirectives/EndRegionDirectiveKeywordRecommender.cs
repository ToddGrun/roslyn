// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#End Region" directive
/// </summary>
internal class EndRegionDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreprocessorEndDirectiveKeywordContext &&
           HasUnmatchedRegionDirective(context, cancellationToken))
        {
            return [new RecommendedKeyword("Region", VBFeaturesResources.Terminates_a_SharpRegion_block)];
        }

        if (context.IsPreprocessorStartContext)
        {
            var directives = context.SyntaxTree.GetStartDirectives(cancellationToken);

            if (HasUnmatchedRegionDirective(context, cancellationToken))
            {
                return [new RecommendedKeyword("#End Region", VBFeaturesResources.Terminates_a_SharpRegion_block)];
            }
        }

        return [];
    }

    private static bool HasUnmatchedRegionDirective(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var directives = context.SyntaxTree.GetStartDirectives(cancellationToken);

        foreach (var directive in directives)
        {
            if (directive.Kind == SyntaxKind.RegionDirectiveTrivia && directive.Span.End <= context.Position)
            {
                if (directive.GetMatchingStartOrEndDirective(cancellationToken) is null)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
