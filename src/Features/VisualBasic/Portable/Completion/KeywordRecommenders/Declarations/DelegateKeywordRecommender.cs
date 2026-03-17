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
/// Recommends the "Delegate" keyword in member declaration contexts
/// </summary>
internal class DelegateKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Delegate", VBFeaturesResources.Used_to_declare_a_delegate_A_delegate_is_a_reference_type_that_refers_to_a_shared_method_of_a_type_or_to_an_instance_method_of_an_object_Any_procedure_that_is_convertible_or_that_has_matching_parameter_types_and_return_type_may_be_used_to_create_an_instance_of_this_delegate_class));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Delegate))
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
