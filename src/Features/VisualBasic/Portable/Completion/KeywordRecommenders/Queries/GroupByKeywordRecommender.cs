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
/// Recommends the "By" keyword for the "Group By" query clause.
/// </summary>
internal class GroupByKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("By", VBFeaturesResources.Specifies_the_element_keys_used_for_grouping_in_Group_By_or_sort_order_in_Order_By)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        if (context.TargetToken.IsChildToken<GroupByClauseSyntax>(static groupBy => groupBy.GroupKeyword) ||
           context.SyntaxTree.IsFollowingCompleteExpression<GroupByClauseSyntax>(
               context.Position, context.TargetToken, static groupBy => groupBy.Items.LastRangeExpression(), cancellationToken))
        {
            return s_keywords;
        }

        return [];
    }
}
