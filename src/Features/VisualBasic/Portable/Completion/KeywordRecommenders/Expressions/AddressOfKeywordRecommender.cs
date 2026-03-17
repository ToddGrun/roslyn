// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "AddressOf" keyword.
/// </summary>
internal class AddressOfKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("AddressOf", VBFeaturesResources.Creates_a_delegate_procedure_instance_that_references_the_specified_procedure_AddressOf_procedureName));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        if (context.IsDelegateCreationContext())
        {
            return s_keywords;
        }

        if (context.IsAnyExpressionContext && !context.TargetToken.Parent.IsKind(SyntaxKind.AddressOfExpression))
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
