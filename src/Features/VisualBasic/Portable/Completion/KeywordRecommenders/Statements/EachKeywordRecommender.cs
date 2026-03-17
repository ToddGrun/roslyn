// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Each" keyword after the "For" keyword
/// </summary>
internal class EachKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Each", VBFeaturesResources.Introduces_a_loop_that_is_repeated_for_each_element_in_a_collection)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsKind(SyntaxKind.ForKeyword) && targetToken.Parent.IsKind(SyntaxKind.ForStatement))
        {
            var forStatement = (ForStatementSyntax)targetToken.Parent;
            if (forStatement.EqualsToken == default || forStatement.EqualsToken.IsMissing)
            {
                return s_keywords;
            }
        }

        return [];
    }
}
