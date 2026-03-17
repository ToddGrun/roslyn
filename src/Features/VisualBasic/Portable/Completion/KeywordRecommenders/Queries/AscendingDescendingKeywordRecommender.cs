// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the "Ascending" and "Descending" contextual keywords in a Order By clause.
/// </summary>
internal class AscendingDescendingKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [
            new RecommendedKeyword("Ascending", VBFeaturesResources.Specifies_the_sort_order_for_an_Order_By_clause_in_a_query_The_smallest_element_will_appear_first),
            new RecommendedKeyword("Descending", VBFeaturesResources.Specifies_the_sort_order_for_an_Order_By_clause_in_a_query_The_largest_element_will_appear_first)
        ];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.SyntaxTree.IsFollowingCompleteExpression<OrderingSyntax>(context.Position, context.TargetToken, static orderingSyntax => orderingSyntax.Expression, cancellationToken)
            ? s_keywords
            : [];
}
