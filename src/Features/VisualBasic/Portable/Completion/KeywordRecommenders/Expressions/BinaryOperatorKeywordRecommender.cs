// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends binary infix operators that are English text, like "AndAlso", "OrElse", "Like", etc.
/// </summary>
internal class BinaryOperatorKeywordRecommender : AbstractKeywordRecommender
{
    internal static readonly ImmutableArray<RecommendedKeyword> KeywordList = ImmutableArray.Create(
        new RecommendedKeyword("And", VBFeaturesResources.Performs_a_logical_conjunction_on_two_Boolean_expressions_or_a_bitwise_conjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_both_operands_evaluate_to_True_Both_expressions_are_always_evaluated_result_expression1_And_expression2),
        new RecommendedKeyword("AndAlso", VBFeaturesResources.Performs_a_short_circuit_logical_conjunction_on_two_expressions_Returns_True_if_both_operands_evaluate_to_True_If_the_first_expression_evaluates_to_False_the_second_is_not_evaluated_result_expression1_AndAlso_expression2),
        new RecommendedKeyword("Or", VBFeaturesResources.Performs_an_inclusive_logical_disjunction_on_two_Boolean_expressions_or_a_bitwise_disjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_at_least_one_operand_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Or_expression2),
        new RecommendedKeyword("OrElse", VBFeaturesResources.Performs_short_circuit_inclusive_logical_disjunction_on_two_expressions_Returns_True_if_either_operand_evaluates_to_True_If_the_first_expression_evaluates_to_True_the_second_expression_is_not_evaluated_result_expression1_OrElse_expression2),
        new RecommendedKeyword("Is", VBFeaturesResources.Compares_two_object_reference_variables_and_returns_True_if_the_objects_are_equal_result_object1_Is_object2),
        new RecommendedKeyword("IsNot", VBFeaturesResources.Compares_two_object_reference_variables_and_returns_True_if_the_objects_are_not_equal_result_object1_IsNot_object2),
        new RecommendedKeyword("Mod", VBFeaturesResources.Divides_two_numbers_and_returns_only_the_remainder_number1_Mod_number2),
        new RecommendedKeyword("Like", VBFeaturesResources.Compares_a_string_against_a_pattern_Wildcards_available_include_to_match_1_character_and_to_match_0_or_more_characters_result_string_Like_pattern),
        new RecommendedKeyword("Xor", VBFeaturesResources.Performs_a_logical_exclusion_on_two_Boolean_expressions_or_a_bitwise_exclusion_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_exactly_one_of_the_expressions_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Xor_expression2));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (IsBinaryOperatorContext(context, cancellationToken))
        {
            return KeywordList;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }

    private static bool IsBinaryOperatorContext(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return false;
        }

        var token = context.TargetToken;

        // Very specific case edge case when the identifier is From or Aggregate. In that case, we'll
        // only show the binary operator keywords if "From" or "Aggregate" binds to a symbol. In that
        // way, we can distinguish between the two following cases:
        //
        // 1.
        // Dim q = From |
        //
        // 2.
        // Dim From = 0
        // Dim q = From |

        if (token.Parent is IdentifierNameSyntax identifierName)
        {
            var text = token.ToString();
            if (SyntaxFacts.GetContextualKeywordKind(text) == SyntaxKind.FromKeyword || SyntaxFacts.GetContextualKeywordKind(text) == SyntaxKind.AggregateKeyword)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
                if (symbol == null)
                {
                    return false;
                }
            }
        }

        // Don't show binary operator keywords in an incomplete Using block
        // Using goo |
        var usingStatement = token.GetAncestor<UsingStatementSyntax>();
        if (usingStatement != null && usingStatement.Expression != null && !usingStatement.Expression.IsMissing)
        {
            if (usingStatement.Expression == token.Parent)
            {
                return false;
            }
        }

        // As a policy, we'll not show them after an object or collection initializer, since we
        // really just want to show "From" or "With"
        if (token.IsFollowingCompleteAsNewClause() ||
            token.IsFollowingCompleteObjectCreationInitializer())
        {
            return false;
        }

        // Binary operators are legal inside a join expression, but we'll show
        // just "Equals" to better guide the user on what they should be
        // typing
        if (context.SyntaxTree.IsFollowingCompleteExpression<JoinConditionSyntax>(
            context.Position, context.TargetToken, static j => j.Left, cancellationToken))
        {
            return false;
        }

        // Binary operators are allowed in cases like
        //
        //    From num In { 1, 2, 3 } Group By a = num |
        //
        // but we will choose to exclude them so the user gets better hints of what they have to
        // type next in the query
        if (context.SyntaxTree.IsFollowingCompleteExpression<ExpressionRangeVariableSyntax>(
            context.Position, context.TargetToken, static j => j.Expression, cancellationToken))
        {
            return false;
        }

        // Some operators (And, Or) are technically legal after an AddressOf expression, but
        // that's unnecessarily pedantic
        if (context.SyntaxTree.IsFollowingCompleteExpression<UnaryExpressionSyntax>(context.Position, context.TargetToken,
            static u => u.Kind() == SyntaxKind.AddressOfExpression ? u : null, cancellationToken))
        {
            return false;
        }

        // In either of these cases:
        //
        //     Dim x(0 |
        //     ReDim y(0 |
        //
        // it's legal to write a binary operator, but in all probability the user wants to write
        // To. Note that if they are writing To then it must be a literal zero, so we'll restrict
        // to that case
        if (token.Kind() == SyntaxKind.IntegerLiteralToken && (int)token.Value == 0)
        {
            if (token.Parent.IsParentKind(SyntaxKind.SimpleArgument))
            {
                var argumentList = token.GetAncestor<ArgumentListSyntax>();
                if (argumentList.Parent != null && (argumentList.Parent.Parent is ReDimStatementSyntax ||
                                                    argumentList.Parent.Parent is VariableDeclaratorSyntax))
                {
                    return false;
                }
            }
        }

        // The expression in an Add/RemoveHandler which specifies the event is just an event, and
        // thus can't get operators applied to it
        if (context.SyntaxTree.IsFollowingCompleteExpression<AddRemoveHandlerStatementSyntax>(
            context.Position, context.TargetToken, static h => h.EventExpression, cancellationToken))
        {
            return false;
        }

        // Exclude from For statements:
        //       For i = 1 |
        // This is legal but is not a good experience in most cases
        if (context.SyntaxTree.IsFollowingCompleteExpression<ForStatementSyntax>(context.Position, context.TargetToken, static forStatement => forStatement.FromValue, cancellationToken))
        {
            return false;
        }

        return context.SyntaxTree.IsFollowingCompleteExpression<ExpressionSyntax>(context.Position, context.TargetToken,
            e =>
            {
                if (context.SyntaxTree.IsExpressionContext(e.SpanStart, cancellationToken, context.SemanticModel))
                {
                    return e;
                }
                else
                {
                    return null;
                }
            }, cancellationToken);
    }
}
