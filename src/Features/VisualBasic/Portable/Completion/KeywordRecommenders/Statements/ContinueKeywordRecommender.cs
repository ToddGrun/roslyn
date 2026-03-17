// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Continue" keyword at the start of a statement when in any loop.
/// </summary>
internal class ContinueKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Continue", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_loop_Can_be_used_in_a_Do_loop_a_For_loop_or_a_While_loop)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            if (context.IsInStatementBlockOfKind(
                SyntaxKind.SimpleDoLoopBlock,
                SyntaxKind.DoWhileLoopBlock,
                SyntaxKind.DoUntilLoopBlock,
                SyntaxKind.DoLoopWhileBlock,
                SyntaxKind.DoLoopUntilBlock,
                SyntaxKind.ForBlock,
                SyntaxKind.ForEachBlock,
                SyntaxKind.WhileBlock))
            {
                return s_keywords;
            }
        }

        return [];
    }
}
