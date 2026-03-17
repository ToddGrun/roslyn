// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Iterator" keyword in member declaration contexts
/// </summary>
internal class IteratorKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Iterator", VBFeaturesResources.Indicates_an_iterator_method_that_can_use_the_Yield_statement));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.IteratorFunction | PossibleDeclarationTypes.IteratorProperty) &&
                modifiers.IteratorKeyword.Kind() == SyntaxKind.None)
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
