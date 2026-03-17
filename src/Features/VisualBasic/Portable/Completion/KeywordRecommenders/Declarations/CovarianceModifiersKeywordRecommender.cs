// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

internal class CovarianceModifiersKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords = ImmutableArray.Create(
        new RecommendedKeyword("In", VBFeaturesResources.Use_In_for_a_type_that_will_only_be_used_for_ByVal_arguments_to_functions),
        new RecommendedKeyword("Out", VBFeaturesResources.Use_Out_for_a_type_that_will_only_be_used_as_a_return_from_functions));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        // No matter what, these can only happen after an Of or a comma
        if (!targetToken.IsKind(SyntaxKind.OfKeyword, SyntaxKind.CommaToken))
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var parent = targetToken.Parent;
        if (parent == null)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        if (parent.IsChildNode<DelegateStatementSyntax>(declaration => declaration.TypeParameterList))
        {
            return s_keywords;
        }
        else if (parent.IsChildNode<TypeStatementSyntax>(declaration => declaration.TypeParameterList))
        {
            if (parent.GetAncestor<TypeStatementSyntax>().IsKind(SyntaxKind.InterfaceStatement))
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
