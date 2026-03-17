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
/// Recommends the "Join" keyword.
/// </summary>
internal class JoinKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Join", VBFeaturesResources.Combines_the_elements_of_two_sequences_The_join_operation_is_based_on_matching_keys)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // First there is the normal and boring "Join"
        if (context.IsQueryOperatorContext || context.IsAdditionalJoinOperatorContext(cancellationToken))
        {
            return s_keywords;
        }

        // Now this might be Group Join...
        var targetToken = context.TargetToken;

        // If it's just "Group" it may have parsed as a Group By
        return targetToken.IsChildToken<GroupByClauseSyntax>(static groupBy => groupBy.GroupKeyword) || targetToken.IsChildToken<GroupJoinClauseSyntax>(static groupBy => groupBy.GroupKeyword)
            ? s_keywords
            : [];
    }
}
