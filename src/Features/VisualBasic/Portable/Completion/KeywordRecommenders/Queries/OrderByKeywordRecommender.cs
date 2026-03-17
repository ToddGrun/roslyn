// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the "Order By" query clause.
/// </summary>
internal class OrderByKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Order By", VBFeaturesResources.Specifies_the_sort_order_for_columns_in_a_query_Can_be_followed_by_either_the_Ascending_or_the_Descending_keyword_If_neither_is_specified_Ascending_is_used)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsQueryOperatorContext ? s_keywords : [];
}
