// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "For" keyword for the statement context
/// </summary>
internal class ForKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            return
            [
                new RecommendedKeyword("For", VBFeaturesResources.Introduces_a_loop_that_is_iterated_a_specified_number_of_times),
                new RecommendedKeyword("For Each", VBFeaturesResources.Introduces_a_loop_that_is_repeated_for_each_element_in_a_collection)
            ];
        }

        // Are we after Exit or Continue?
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) &&
           context.IsInStatementBlockOfKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock) &&
           !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            if (targetToken.IsKind(SyntaxKind.ExitKeyword))
            {
                return [new RecommendedKeyword("For", VBFeaturesResources.Exits_a_For_loop_and_transfers_execution_immediately_to_the_statement_following_the_Next_statement)];
            }
            else
            {
                return [new RecommendedKeyword("For", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_For_loop)];
            }
        }

        return [];
    }
}
