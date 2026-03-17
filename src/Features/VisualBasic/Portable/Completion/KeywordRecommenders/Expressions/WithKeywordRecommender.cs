// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "With" keyword when used in a New syntax (such as New goo With)
/// </summary>
internal class WithKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("With", VBFeaturesResources.Specifies_the_declaration_of_property_initializations_in_an_object_initializer_New_typeName_With_bracket_property_expression_bracket_bracket_bracket));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var token = context.TargetToken;
        if (token.IsChildToken<AsNewClauseSyntax>(asNewClause => asNewClause.NewExpression.NewKeyword) ||
           token.IsFollowingCompleteAsNewClause() ||
           token.IsChildToken<ObjectCreationExpressionSyntax>(objectCreation => objectCreation.NewKeyword) ||
           token.IsFollowingCompleteObjectCreation())
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
