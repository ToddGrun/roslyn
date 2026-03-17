// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "New" keyword.
/// </summary>
internal class NewKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("New", VBFeaturesResources.Creates_a_new_object_instance));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsAnyExpressionContext)
        {
            return s_keywords;
        }

        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsChildToken<AsClauseSyntax>(asClause => asClause.AsKeyword))
        {
            var asClause = targetToken.GetAncestor<AsClauseSyntax>();
            if (asClause.IsParentKind(SyntaxKind.VariableDeclarator) ||
               (asClause.IsParentKind(SyntaxKind.PropertyStatement) &&
                !((PropertyStatementSyntax)asClause.Parent).Modifiers.Any(
                    m => m.IsKind(SyntaxKind.WriteOnlyKeyword))))
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
