// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Case" and possibly "Case Else" keyword inside a Select block
/// </summary>
internal class CaseKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;

        // Are we after "Select" for "Select Case"?
        if (targetToken.Kind == SyntaxKind.SelectKeyword &&
           !targetToken.Parent.IsKind(SyntaxKind.SelectClause) &&
           !context.FollowsEndOfStatement)
        {
            return [new RecommendedKeyword("Case", VBFeaturesResources.Introduces_a_value_or_set_of_values_against_which_the_value_of_an_expression_in_a_Select_Case_statement_is_to_be_tested_Case_expression_expression1_To_expression2_bracket_Is_bracket_comparisonOperator_expression)];
        }

        // A "Case" keyword must be in a Select block, and exists either where a regular executable statement can go
        // or the special case of being immediately after the Select Case
        if (!context.IsInStatementBlockOfKind(SyntaxKind.SelectBlock) ||
           !(context.IsMultiLineStatementContext || context.IsAfterStatementOfKind(SyntaxKind.SelectStatement)))
        {
            return [];
        }

        var selectStatement = targetToken.GetAncestor<SelectBlockSyntax>();
        var validKeywords = new List<RecommendedKeyword>();

        // We can do "Case" as long as we're not after a "Case Else"
        var caseElseBlock = selectStatement.CaseBlocks.FirstOrDefault(caseBlock => caseBlock.CaseStatement.Kind == SyntaxKind.CaseElseStatement);
        if (caseElseBlock is null || targetToken.SpanStart < caseElseBlock.SpanStart)
        {
            validKeywords.Add(new RecommendedKeyword("Case", VBFeaturesResources.Introduces_a_value_or_set_of_values_against_which_the_value_of_an_expression_in_a_Select_Case_statement_is_to_be_tested_Case_expression_expression1_To_expression2_bracket_Is_bracket_comparisonOperator_expression));
        }

        // We can do a "Case Else" as long as we're the last one and we don't already have one.
        // We exclude any partial case keywords the parser is creating (possibly because of user typing)
        var lastBlock = selectStatement.CaseBlocks.LastOrDefault(caseBlock => !caseBlock.CaseStatement.CaseKeyword.IsMissing);
        if (caseElseBlock is null && (lastBlock is null || targetToken.SpanStart > lastBlock.SpanStart))
        {
            validKeywords.Add(new RecommendedKeyword("Case Else", VBFeaturesResources.Introduces_the_statements_to_run_if_none_of_the_previous_cases_in_the_Select_Case_statement_returns_True));
        }

        return validKeywords.ToImmutableArray();
    }
}
