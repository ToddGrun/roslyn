// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements;

/// <summary>
/// Recommends "Next" after "On Error Resume" or after the "Resume" statement
/// </summary>
internal class NextKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        return targetToken.IsKind(SyntaxKind.ResumeKeyword) && !context.IsInLambda && targetToken.Parent.IsKind(SyntaxKind.OnErrorResumeNextStatement, SyntaxKind.ResumeStatement, SyntaxKind.ResumeNextStatement)
            ? s_keywords
            : [];
    }
}
