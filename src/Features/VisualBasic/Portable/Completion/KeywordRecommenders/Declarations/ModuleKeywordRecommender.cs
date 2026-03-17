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
/// Recommends the "Module" keyword in type declaration contexts
/// </summary>
internal class ModuleKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Module", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_module_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SyntaxTree.IsDeclarationContextWithinTypeBlocks(context.Position, context.TargetToken, true, cancellationToken, SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock))
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Module))
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
