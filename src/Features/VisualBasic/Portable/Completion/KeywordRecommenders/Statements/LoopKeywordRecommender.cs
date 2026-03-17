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
/// Recommends the "Loop" statement.
/// </summary>
internal class LoopKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;

        if (context.IsStatementContext)
        {
            var doBlock = targetToken.GetAncestor<DoLoopBlockSyntax>();

            if (doBlock is null || !doBlock.LoopStatement.IsMissing)
            {
                return [];
            }

            if (doBlock.Kind != SyntaxKind.SimpleDoLoopBlock)
            {
                return [new RecommendedKeyword("Loop", VBFeaturesResources.Terminates_a_loop_that_is_introduced_with_a_Do_statement)];
            }
            else
            {
                return
                [
                    new RecommendedKeyword("Loop", VBFeaturesResources.Terminates_a_loop_that_is_introduced_with_a_Do_statement),
                    new RecommendedKeyword("Loop Until", VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Loop_Until_condition),
                    new RecommendedKeyword("Loop While", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_Loop_While_condition)
                ];
            }
        }

        return [];
    }
}
