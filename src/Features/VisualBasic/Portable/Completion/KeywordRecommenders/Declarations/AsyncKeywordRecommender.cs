// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

internal class AsyncKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Async", VBFeaturesResources.Indicates_an_asynchronous_method_that_can_use_the_Await_operator));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // Function/Sub declaration
        if (context.IsTypeMemberDeclarationKeywordContext || context.IsInterfaceMemberDeclarationKeywordContext)
        {
            if (context.ModifierCollectionFacts.AsyncKeyword.Kind() == SyntaxKind.None &&
                context.ModifierCollectionFacts.IteratorKeyword.Kind() == SyntaxKind.None &&
                context.ModifierCollectionFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.PartialKeyword &&
                context.ModifierCollectionFacts.MutabilityOrWithEventsKeyword.Kind() == SyntaxKind.None)
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
