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
/// Recommends the "While" and "Until" keywords as a part of a Do or Loop statements
/// </summary>
internal class UntilAndWhileKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        if ((targetToken.Kind == SyntaxKind.DoKeyword && targetToken.Parent is DoStatementSyntax) ||
         (targetToken.Kind == SyntaxKind.LoopKeyword &&
         targetToken.Parent is LoopStatementSyntax &&
         targetToken.Parent.Parent.IsKind(SyntaxKind.SimpleDoLoopBlock, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock)))
        {
            return
            [
                new RecommendedKeyword("Until", targetToken.Kind == SyntaxKind.LoopKeyword
                                                   ? VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Loop_Until_condition
                                                   : VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Until_condition_Loop),
                new RecommendedKeyword("While", targetToken.Kind == SyntaxKind.LoopKeyword
                                                   ? VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_Loop_While_condition
                                                   : VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_While_condition_Loop)
            ];
        }
        else
        {
            return [];
        }
    }
}
