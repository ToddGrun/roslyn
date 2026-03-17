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
/// Recommends the "Step" keyword in a For statement.
/// </summary>
internal class StepKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Step", VBFeaturesResources.Specifies_how_much_to_increment_between_each_loop_iteration)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SyntaxTree.IsFollowingCompleteExpression<ForStatementSyntax>(
            context.Position,
            context.TargetToken,
            static forStatement => forStatement.ToValue,
            cancellationToken,
            allowImplicitLineContinuation: false))
        {
            return s_keywords;
        }
        else
        {
            return [];
        }
    }
}
