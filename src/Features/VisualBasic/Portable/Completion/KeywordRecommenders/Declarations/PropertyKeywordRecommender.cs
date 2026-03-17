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
/// Recommends the "Property" keyword in member declaration contexts
/// </summary>
internal class PropertyKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Property", VBFeaturesResources.Declares_the_name_of_a_property_and_the_property_procedures_used_to_store_and_retrieve_the_value_of_the_property));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeMemberDeclarationKeywordContext || context.IsInterfaceMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Property | PossibleDeclarationTypes.IteratorProperty))
            {
                return s_keywords;
            }
        }

        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) &&
            context.IsInStatementBlockOfKind(SyntaxKind.PropertyBlock) &&
            !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
