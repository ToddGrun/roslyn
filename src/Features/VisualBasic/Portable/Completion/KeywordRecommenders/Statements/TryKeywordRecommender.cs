// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Try" keyword for the statement context
/// </summary>
internal class TryKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMultiLineStatementContext)
        {
            return [new RecommendedKeyword("Try", VBFeaturesResources.Provides_a_way_to_handle_some_or_all_possible_errors_that_might_occur_in_a_given_block_of_code_while_still_running_the_code_Try_bracket_Catch_bracket_Catch_Finally_End_Try)];
        }

        // Are we after Exit or Continue (but not in a Finally block)?
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKind(SyntaxKind.ExitKeyword) &&
           context.IsInStatementBlockOfKind(SyntaxKind.TryBlock) &&
           !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return [new RecommendedKeyword("Try", VBFeaturesResources.Exits_a_Try_block_and_transfers_execution_immediately_to_the_statement_following_the_End_Try_statement)];
        }

        return [];
    }
}
