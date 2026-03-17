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
/// Recommends the "Then" keyword in an If statement.
/// </summary>
internal class ThenKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Then", VBFeaturesResources.Introduces_a_statement_block_to_be_compiled_or_executed_if_a_tested_condition_is_true)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var isFollowingIfStatement = context.SyntaxTree.IsFollowingCompleteExpression<IfStatementSyntax>(
            context.Position,
            context.TargetToken,
            childGetter: static ifStatement => ifStatement.Condition,
            cancellationToken: cancellationToken,
            allowImplicitLineContinuation: false);

        var isFollowingIfDirective = context.IsPreProcessorDirectiveContext &&
            context.SyntaxTree.IsFollowingCompleteExpression<IfDirectiveTriviaSyntax>(
                context.Position,
                context.TargetToken,
                childGetter: static ifDirective => ifDirective.Condition,
                cancellationToken: cancellationToken,
                allowImplicitLineContinuation: false);

        if (isFollowingIfStatement || isFollowingIfDirective)
        {
            return s_keywords;
        }

        return [];
    }
}
