// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "SyncLock" keyword for the statement context
/// </summary>
internal class SyncLockKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("SyncLock", VBFeaturesResources.Ensures_that_multiple_threads_do_not_execute_the_statement_block_at_the_same_time_SyncLock_object_End_Synclock)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsMultiLineStatementContext ? s_keywords : [];
}
