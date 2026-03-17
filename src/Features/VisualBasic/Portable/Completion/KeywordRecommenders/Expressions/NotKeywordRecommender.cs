// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "Not" keyword.
/// </summary>
internal class NotKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Not", VBFeaturesResources.Performs_logical_negation_on_a_Boolean_expression_or_bitwise_negation_on_a_numeric_expression_result_Not_expression));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsAnyExpressionContext ? s_keywords : ImmutableArray<RecommendedKeyword>.Empty;
    }
}
