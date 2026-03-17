// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the Skip operator for Skip/Skip While.
/// </summary>
internal class SkipKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Skip", VBFeaturesResources.Skips_elements_up_to_a_specified_position_in_the_collection)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsQueryOperatorContext ? s_keywords : [];
}
