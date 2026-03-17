// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements;

/// <summary>
/// Recommends "Error" after "On" in a "On Error" statement.
/// </summary>
internal class ErrorKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;

        if (targetToken.IsKind(SyntaxKind.OnKeyword) && IsOnErrorStatement(targetToken.Parent))
        {
            return ImmutableArray.Create(
                new RecommendedKeyword("Error Resume Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error),
                new RecommendedKeyword("Error GoTo", VBFeaturesResources.Enables_the_error_handling_routine_that_starts_at_the_line_specified_in_the_line_argument_The_specified_line_must_be_in_the_same_procedure_as_the_On_Error_statement_On_Error_GoTo_bracket_label_0_1_bracket));
        }

        // The Error statement (i.e. "Error 11" to raise an error)
        if (context.IsMultiLineStatementContext || context.IsStatementContext)
        {
            return ImmutableArray.Create(new RecommendedKeyword("Error", VBFeaturesResources.Simulates_the_occurrence_of_an_error));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
