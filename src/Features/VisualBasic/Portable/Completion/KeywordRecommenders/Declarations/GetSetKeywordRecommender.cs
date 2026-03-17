// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Get" and "Set" keyword in property declarations.
/// </summary>
internal class GetSetKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // If we have modifiers which exclude it, then definitely not
        var modifiers = context.ModifierCollectionFacts;
        if (!modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Accessor))
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        // Are we following the property declaration?
        var previousToken = targetToken;
        while (previousToken.IsModifier())
        {
            previousToken = previousToken.GetPreviousToken();
        }

        var propertyBlock = previousToken.GetAncestor<PropertyBlockSyntax>();
        var propertyDeclaration = previousToken.GetAncestor<PropertyStatementSyntax>();
        var accessorBlock = previousToken.GetAncestors<SyntaxNode>().FirstOrDefault(ancestor => ancestor.IsKind(SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock));

        if (propertyBlock != null && propertyDeclaration == null)
        {
            propertyDeclaration = propertyBlock.PropertyStatement;
        }

        var getAllowed = false;
        var setAllowed = false;

        if (propertyDeclaration != null)
        {
            if (!propertyDeclaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.ReadOnlyKeyword))
            {
                setAllowed = true;
            }

            if (!propertyDeclaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.WriteOnlyKeyword))
            {
                getAllowed = true;
            }
        }

        // If we're already after a previous accessor, then exclude it
        if (accessorBlock.IsKind(SyntaxKind.GetAccessorBlock))
        {
            getAllowed = false;
        }

        if (accessorBlock.IsKind(SyntaxKind.SetAccessorBlock))
        {
            setAllowed = false;
        }

        var recommendations = new List<RecommendedKeyword>();

        if (getAllowed)
        {
            recommendations.Add(new RecommendedKeyword("Get", VBFeaturesResources.Declares_a_Get_property_procedure_that_is_used_to_return_the_current_value_of_a_property));
        }

        if (setAllowed)
        {
            recommendations.Add(new RecommendedKeyword("Set", VBFeaturesResources.Declares_a_Set_property_procedure_that_is_used_to_assign_a_value_to_a_property));
        }

        return recommendations.ToImmutableArray();
    }
}
