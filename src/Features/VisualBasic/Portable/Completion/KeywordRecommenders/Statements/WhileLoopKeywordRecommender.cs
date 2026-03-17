// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "While" keyword at the start of a statement. "While" as a part of a Do statement is handled in
/// the UntilAndWhileKeywordRecommender.
/// </summary>
internal class WhileLoopKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            return [new RecommendedKeyword("While", VBFeaturesResources.Runs_a_series_of_statements_as_long_as_a_given_condition_is_true)];
        }

        // Are we after Exit or Continue?
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) &&
           context.IsInStatementBlockOfKind(SyntaxKind.WhileBlock) &&
           !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return
            [
                new RecommendedKeyword("While",
                    targetToken.IsKind(SyntaxKind.ExitKeyword)
                       ? VBFeaturesResources.Exits_a_While_loop_and_transfers_execution_immediately_to_the_statement_following_the_End_While_statement
                       : VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_While_loop)
            ];
        }

        return [];
    }
}
