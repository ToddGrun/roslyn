// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Assembly" and "Module" keyword for top-level attributes that may exist in a file.
/// </summary>
internal class AttributeScopesKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(
            new RecommendedKeyword("Assembly", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_assembly_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property),
            new RecommendedKeyword("Module", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_module_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsKind(SyntaxKind.LessThanToken) &&
            targetToken.IsChildToken<AttributeListSyntax>(block => block.LessThanToken))
        {
            var attributeList = targetToken.Parent;
            if (attributeList.Parent.IsKind(SyntaxKind.AttributesStatement))
            {
                return s_keywords;
            }

            if (attributeList.GetAncestors<DeclarationStatementSyntax>().Count() == 1)
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
