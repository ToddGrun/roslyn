// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements;

/// <summary>
/// Recommends "Resume Next" after "On Error", or "Resume" as a standalone statement
/// </summary>
internal class ResumeKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // On Error statements are never valid in lambdas
        if (context.IsInLambda)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        if (targetToken.Kind == SyntaxKind.ErrorKeyword && IsOnErrorStatement(targetToken.Parent))
        {
            return
            [
                new RecommendedKeyword("Resume Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error)
            ];
        }

        if (context.IsMultiLineStatementContext)
        {
            return
            [
                new RecommendedKeyword("Resume", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error)
            ];
            // TODO: we are inconsistent here in Dev10. We offer "On Error Resume Next" even after typing just "On",
            // yet curiously we don't show "Resume Next" as it's own statement. This might be something to fix if
            // we determine we even care.
        }

        return [];
    }
}
