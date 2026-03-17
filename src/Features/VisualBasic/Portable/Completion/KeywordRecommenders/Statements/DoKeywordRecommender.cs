// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Do" keyword at the start of a statement
/// </summary>
internal class DoKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            return
            [
                new RecommendedKeyword("Do", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_or_until_the_condition_becomes_true_Do_Loop_While_Until_condition),
                new RecommendedKeyword("Do Until", VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Until_condition_Loop),
                new RecommendedKeyword("Do While", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_While_condition_Loop)
            ];
        }

        // Are we after Exit or Continue?
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) &&
           context.IsInStatementBlockOfKind(SyntaxKind.SimpleDoLoopBlock,
                                            SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
                                            SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock) &&
           !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            if (targetToken.IsKind(SyntaxKind.ExitKeyword))
            {
                return [new RecommendedKeyword("Do", VBFeaturesResources.Exits_a_Do_loop_and_transfers_execution_immediately_to_the_statement_following_the_Loop_statement)];
            }
            else
            {
                return [new RecommendedKeyword("Do", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_Do_loop)];
            }
        }

        return [];
    }
}
