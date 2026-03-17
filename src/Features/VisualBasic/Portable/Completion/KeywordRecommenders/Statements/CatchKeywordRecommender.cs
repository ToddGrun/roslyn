// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Catch" keyword for the statement context
/// </summary>
internal class CatchKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Catch", VBFeaturesResources.Introduces_a_statement_block_to_be_run_if_the_specified_exception_occurs_inside_a_Try_block)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (!context.IsMultiLineStatementContext)
        {
            return [];
        }

        // We'll recommend a catch statement if it's within a Try block or a Catch block, because you could be
        // trying to start one in either location
        return context.IsInStatementBlockOfKind(SyntaxKind.TryBlock, SyntaxKind.CatchBlock) && !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock)
            ? s_keywords
            : [];
    }
}
