// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.ArrayStatements;

/// <summary>
/// Recommends the "Preserve" modifier after the ReDim statement.
/// </summary>
internal class PreserveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Preserve", VBFeaturesResources.Prevents_the_contents_of_an_array_from_being_cleared_when_the_dimensions_of_the_array_are_changed));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (targetToken.Kind() == SyntaxKind.ReDimKeyword && targetToken.IsChildToken<ReDimStatementSyntax>(statement => statement.ReDimKeyword))
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
