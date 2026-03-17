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
/// Recommends the "On" keyword.
/// </summary>
internal class OnKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("On", VBFeaturesResources.Specifies_the_element_keys_used_to_correlate_sequences_for_a_join_operation)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SyntaxTree.IsFollowingCompleteExpression<JoinClauseSyntax>(context.Position, context.TargetToken, static joinQuery => joinQuery.JoinedVariables.LastCollectionExpression, cancellationToken) ||
           context.SyntaxTree.IsFollowingCompleteExpression<JoinConditionSyntax>(context.Position, context.TargetToken, static joinCondition => joinCondition.Right, cancellationToken))
        {
            var token = context.TargetToken.GetPreviousToken();

            // There must be at least one Join clause in this query which doesn't have an On statement. We also recommend
            // it if the parser has already placed this On in the tree.
            foreach (var joinClause in token.GetAncestors<JoinClauseSyntax>())
            {
                if (joinClause.OnKeyword.IsMissing || joinClause.OnKeyword == token)
                {
                    return s_keywords;
                }
            }
        }

        return [];
    }
}
