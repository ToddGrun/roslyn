// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Select" keyword at the start of a statement
/// </summary>
internal class SelectKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            return [new RecommendedKeyword("Select", VBFeaturesResources.Runs_one_of_several_groups_of_statements_depending_on_the_value_of_an_expression)];
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKind(SyntaxKind.ExitKeyword) &&
           context.IsInStatementBlockOfKind(SyntaxKind.SelectBlock) &&
           !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return [new RecommendedKeyword("Select", VBFeaturesResources.Exits_a_Select_block_and_transfers_execution_immediately_to_the_statement_following_the_End_Select_statement)];
        }

        return [];
    }
}
