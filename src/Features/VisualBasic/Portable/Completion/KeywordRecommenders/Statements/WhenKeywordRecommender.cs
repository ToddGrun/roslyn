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
/// Recommends the "When" keyword for a Catch filter
/// </summary>
internal class WhenKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("When", VBFeaturesResources.Adds_a_conditional_test_to_a_Catch_statement_Exceptions_are_caught_by_that_Catch_statement_only_when_the_conditional_test_that_follows_the_When_keyword_evaluates_to_True)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsFromIdentifierNode<CatchStatementSyntax>(static catchStatement => catchStatement.IdentifierName))
        {
            return s_keywords;
        }

        if (context.SyntaxTree.IsFollowingCompleteExpression<SimpleAsClauseSyntax>(context.Position, context.TargetToken,
            childGetter: static asClause => asClause.Parent is CatchStatementSyntax ? asClause.Type : null, cancellationToken: cancellationToken))
        {
            return s_keywords;
        }

        return [];
    }
}
