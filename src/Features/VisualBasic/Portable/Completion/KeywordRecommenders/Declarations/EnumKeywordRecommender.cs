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
/// Recommends the "Enum" keyword in type declaration contexts
/// </summary>
internal class EnumKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Enum", VBFeaturesResources.Declares_an_enumeration_and_defines_the_values_of_its_members));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Enum))
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
