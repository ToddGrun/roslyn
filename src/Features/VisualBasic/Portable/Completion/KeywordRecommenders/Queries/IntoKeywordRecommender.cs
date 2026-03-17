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
/// Recommends the "Into" keyword.
/// </summary>
internal class IntoKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Into", VBFeaturesResources.Specifies_an_identifier_that_can_serve_as_a_reference_to_the_results_of_a_join_or_grouping_subexpression)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // "Into" for Group By is easy
        if (context.SyntaxTree.IsFollowingCompleteExpression<GroupByClauseSyntax>(
           context.Position, context.TargetToken, static g => g.Keys.LastRangeExpression(), cancellationToken))
        {
            return s_keywords;
        }

        // "Into" for Group Join is also easy
        if (context.SyntaxTree.IsFollowingCompleteExpression<GroupJoinClauseSyntax>(
           context.Position, context.TargetToken, static g => g.JoinConditions.LastJoinKey(), cancellationToken))
        {
            return s_keywords;
        }

        // "Into" for Aggregate is annoying, since it can be following after any number of arbitrary clauses
        if (context.IsQueryOperatorContext)
        {
            var token = context.TargetToken.GetPreviousToken();
            var aggregateQuery = token.GetAncestor<AggregateClauseSyntax>();

            if (aggregateQuery is not null && (aggregateQuery.IntoKeyword.IsMissing || aggregateQuery.IntoKeyword == token))
            {
                return s_keywords;
            }
        }

        return [];
    }
}
