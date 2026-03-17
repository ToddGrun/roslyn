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
/// Recommends the "Else" keyword for the statement context.
/// </summary>
internal class ElseKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;
        var parent = targetToken.GetAncestor<SingleLineIfStatementSyntax>();

        if (parent is not null && !parent.Statements.IsEmpty())
        {
            if (context.IsFollowingCompleteStatement<SingleLineIfStatementSyntax>(static ifStatement => ifStatement.Statements.Last()))
            {
                return [new RecommendedKeyword("Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_If_statement_that_is_executed_if_no_previous_condition_evaluates_to_True)];
            }
        }

        if (context.IsStatementContext &&
           IsDirectlyInIfOrElseIf(context))
        {
            return [new RecommendedKeyword("Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_If_statement_that_is_executed_if_no_previous_condition_evaluates_to_True)];
        }

        // Determine whether we can offer "Else" after "Case" in a Select block.
        if (targetToken.Kind == SyntaxKind.CaseKeyword && targetToken.Parent.IsKind(SyntaxKind.CaseStatement))
        {
            // Next, grab the parenting "Select" block and ensure that it doesn't have any Case Else statements
            var selectBlock = targetToken.GetAncestor<SelectBlockSyntax>();
            if (selectBlock is not null &&
               !selectBlock.CaseBlocks.Any(cb => cb.CaseStatement.Kind == SyntaxKind.CaseElseStatement))
            {
                // Finally, ensure this case statement is the last one in the parenting "Select" block.
                if (selectBlock.CaseBlocks.Last().CaseStatement == targetToken.Parent)
                {
                    return [new RecommendedKeyword("Else", VBFeaturesResources.Introduces_the_statements_to_run_if_none_of_the_previous_cases_in_the_Select_Case_statement_returns_True)];
                }
            }
        }

        return [];
    }

    private static bool IsDirectlyInIfOrElseIf(VisualBasicSyntaxContext context)
    {
        // Maybe we're after the Then keyword
        if (context.TargetToken.IsKind(SyntaxKind.ThenKeyword) &&
            context.TargetToken.Parent?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) == true)
        {
            return true;
        }

        var statement = context.TargetToken.Parent.GetAncestor<StatementSyntax>();
        return statement?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) ?? false;
    }
}
