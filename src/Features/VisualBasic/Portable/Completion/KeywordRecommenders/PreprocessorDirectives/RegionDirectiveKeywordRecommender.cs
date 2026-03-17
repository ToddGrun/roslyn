// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#Region" directive
/// </summary>
internal class RegionDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("#Region", VBFeaturesResources.Collapses_and_hides_sections_of_code_in_Visual_Basic_files)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsPreprocessorStartContext && !context.SyntaxTree.IsEnumMemberNameContext(context)
            ? s_keywords
            : [];
}
