// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Using" keyword at the beginning of a statement.
/// </summary>
internal class UsingKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Using", VBFeaturesResources.A_Using_block_does_three_things_colon_it_creates_and_initializes_variables_in_the_resource_list_it_runs_the_code_in_the_block_and_it_disposes_of_the_variables_before_exiting_Resources_used_in_the_Using_block_must_implement_System_IDisposable_Using_resource1_bracket_resource2_bracket_End_Using)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsMultiLineStatementContext ? s_keywords : [];
}
