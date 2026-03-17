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
/// Recommends the "If" keyword for the statement context
/// </summary>
internal class IfKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsStatementContext)
        {
            return [new RecommendedKeyword("If", VBFeaturesResources.Conditionally_executes_a_group_of_statements_depending_on_the_value_of_an_expression)];
        }

        // We might be typing "Else If" as two keywords. At this point, the parser is parsing this statement as a
        // ElseStatementSyntax.
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsChildToken<ElseStatementSyntax>(static ifStatement => ifStatement.ElseKeyword))
        {
            return [new RecommendedKeyword("If", VBFeaturesResources.Introduces_a_condition_in_an_If_statement_that_is_to_be_tested_if_the_previous_conditional_test_fails)];
        }

        return [];
    }
}
