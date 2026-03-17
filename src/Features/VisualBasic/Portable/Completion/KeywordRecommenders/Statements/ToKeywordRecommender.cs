// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "To" keyword in an For.
/// </summary>
internal class ToKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("To", VBFeaturesResources.Separates_the_beginning_and_ending_values_of_a_loop_counter_or_array_bounds_or_that_of_a_value_match_range)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        // Case statements. We must check for what the parser would parse both with and without
        // the To statement.
        if (context.SyntaxTree.IsFollowingCompleteExpression<SimpleCaseClauseSyntax>(context.Position, context.TargetToken, static c => c.Value, cancellationToken) ||
           context.SyntaxTree.IsFollowingCompleteExpression<RangeCaseClauseSyntax>(context.Position, context.TargetToken, static c => c.LowerBound, cancellationToken))
        {
            return s_keywords;
        }

        if (context.SyntaxTree.IsFollowingCompleteExpression<ForStatementSyntax>(context.Position, context.TargetToken, static forStatement => forStatement.FromValue, cancellationToken))
        {
            return s_keywords;
        }

        return [];
    }
}
