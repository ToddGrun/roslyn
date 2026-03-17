// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Stop" statement.
/// </summary>
internal class StopKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Stop", VBFeaturesResources.Suspends_program_execution)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsStatementContext ? s_keywords : [];
}
