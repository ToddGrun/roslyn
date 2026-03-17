// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the Equals keyword when in a join syntax.
/// </summary>
internal class EqualsKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Equals", VBFeaturesResources.Specifies_the_relationship_between_element_keys_to_use_as_the_basis_of_a_join_operation)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.SyntaxTree.IsFollowingCompleteExpression<JoinConditionSyntax>(context.Position, context.TargetToken, static joinCondition => joinCondition.Left, cancellationToken)
            ? s_keywords
            : [];
}
