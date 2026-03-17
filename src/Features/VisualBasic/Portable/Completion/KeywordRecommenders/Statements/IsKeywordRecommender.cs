// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Is" keyword at the beginning of any clause in a "Case" statement
/// </summary>
internal class IsKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Is", VBFeaturesResources.Followed_by_a_comparison_operator_and_then_an_expression_Case_Is_introduces_the_statements_to_run_if_the_Select_Case_expression_combined_with_the_Case_Is_expression_evaluates_to_True)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        // Determine whether we can offer "Is" at the beginning of a CaseClauseSyntax. Make sure
        // that the token is not after a CaseElseStatement.
        var selectBlock = targetToken.GetAncestor<SelectBlockSyntax>();
        if (selectBlock is not null)
        {
            var caseElseBlock = selectBlock.CaseBlocks.FirstOrDefault(caseBlock => caseBlock.CaseStatement.Kind == SyntaxKind.CaseElseStatement);
            if (caseElseBlock is null || targetToken.SpanStart < caseElseBlock.SpanStart)
            {
                // Handle cases where the token is at the beginning of the first clause
                // (following the Case keyword) and where the token is at the beginning of
                // subsequent clauses (following the list separator token).
                if (targetToken.IsChildToken<CaseStatementSyntax>(static caseStatement => caseStatement.CaseKeyword) ||
                   targetToken.IsChildSeparatorToken<CaseStatementSyntax, CaseClauseSyntax>(static caseStatement => caseStatement.Cases))
                {
                    return s_keywords;
                }
            }
        }

        return [];
    }
}
