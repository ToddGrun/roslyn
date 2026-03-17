// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Property" keyword in member declaration contexts
/// </summary>
internal class ModifierKeywordsRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (!context.IsTypeMemberDeclarationKeywordContext && !context.IsTypeDeclarationKeywordContext)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var modifierFacts = context.ModifierCollectionFacts;
        var recommendations = new List<RecommendedKeyword>();
        var targetToken = context.TargetToken;

        var innermostDeclaration = GetInnermostDeclarationContext(targetToken);
        var innermostDeclarationKind =
            innermostDeclaration != null && innermostDeclaration.Kind() != SyntaxKind.CompilationUnit
                ? innermostDeclaration.Kind()
                : SyntaxKind.NamespaceBlock;

        if (modifierFacts.AccessibilityKeyword.Kind() == SyntaxKind.None && !context.IsInterfaceMemberDeclarationKeywordContext)
        {
            if (modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.PartialKeyword)
            {
                recommendations.Add(new RecommendedKeyword("Public", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_have_no_access_restrictions));
            }

            // Only "Public" is legal for operators
            if (modifierFacts.NarrowingOrWideningKeyword.Kind() == SyntaxKind.None)
            {
                if (modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.PartialKeyword)
                {
                    recommendations.Add(new RecommendedKeyword("Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_the_assembly_that_contains_their_declaration));
                }

                if (modifierFacts.DefaultKeyword.Kind() == SyntaxKind.None && innermostDeclarationKind != SyntaxKind.NamespaceBlock)
                {
                    recommendations.Add(new RecommendedKeyword("Private", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_module_class_or_structure));
                }
            }
        }

        if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.ProtectedMember))
        {
            if (modifierFacts.AccessibilityKeyword.Kind() == SyntaxKind.None)
            {
                recommendations.Add(new RecommendedKeyword("Protected", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_own_class_or_from_a_derived_class));
                recommendations.Add(new RecommendedKeyword("Protected Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_members_of_a_class_are_accessible_from_anywhere_in_the_same_assembly_their_own_classes_and_derived_classes));
            }
            else if (modifierFacts.AccessibilityKeyword.Kind() == SyntaxKind.ProtectedKeyword && !modifierFacts.HasProtectedAndFriend)
            {
                // We could still have a "Friend" later
                recommendations.Add(new RecommendedKeyword("Friend", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_the_assembly_that_contains_their_declaration));
            }
            else if (modifierFacts.AccessibilityKeyword.Kind() == SyntaxKind.FriendKeyword && !modifierFacts.HasProtectedAndFriend)
            {
                // We could still have a "Protected" later
                recommendations.Add(new RecommendedKeyword("Protected", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_accessible_only_from_within_their_own_class_or_from_a_derived_class));
            }
        }

        // Show "Partial" at the module level. Recommending it before "Private"
        // is fine, because we'll prettylist it.
        if (innermostDeclarationKind == SyntaxKind.ClassBlock ||
            innermostDeclarationKind == SyntaxKind.ModuleBlock ||
            innermostDeclarationKind == SyntaxKind.StructureBlock ||
            innermostDeclarationKind == SyntaxKind.NamespaceBlock)
        {
            if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class) &&
                modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.PartialKeyword &&
                modifierFacts.AsyncKeyword.Kind() == SyntaxKind.None &&
                modifierFacts.IteratorKeyword.Kind() == SyntaxKind.None)
            {
                recommendations.Add(new RecommendedKeyword("Partial", VBFeaturesResources.Indicates_that_a_method_class_or_structure_declaration_is_a_partial_definition_of_the_method_class_or_structure));
            }
        }

        if (modifierFacts.AsyncKeyword.Kind() == SyntaxKind.None &&
            modifierFacts.IteratorKeyword.Kind() == SyntaxKind.None)
        {
            if (modifierFacts.MutabilityOrWithEventsKeyword.Kind() == SyntaxKind.None)
            {
                if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field))
                {
                    recommendations.Add(new RecommendedKeyword("Const", VBFeaturesResources.Declares_and_defines_one_or_more_constants));
                    recommendations.Add(new RecommendedKeyword("WithEvents", VBFeaturesResources.Specifies_that_one_or_more_declared_member_variables_refer_to_an_instance_of_a_class_that_can_raise_events));
                }

                if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) || modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field))
                {
                    recommendations.Add(new RecommendedKeyword("ReadOnly", VBFeaturesResources.Specifies_that_a_variable_or_property_can_be_read_but_not_written_to));
                }

                if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property))
                {
                    recommendations.Add(new RecommendedKeyword("WriteOnly", VBFeaturesResources.Specifies_that_a_property_can_be_written_to_but_not_read));
                }
            }

            // Some modifiers cannot appear at the module level
            if (innermostDeclarationKind == SyntaxKind.ClassBlock ||
                innermostDeclarationKind == SyntaxKind.InterfaceBlock ||
                innermostDeclarationKind == SyntaxKind.StructureBlock ||
                innermostDeclarationKind == SyntaxKind.NamespaceBlock)
            {
                if (modifierFacts.InheritenceKeyword.Kind() == SyntaxKind.None && modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class))
                {
                    recommendations.Add(new RecommendedKeyword("MustInherit", VBFeaturesResources.Specifies_that_a_class_can_be_used_only_as_a_base_class_and_that_you_cannot_create_an_object_directly_from_it));
                    recommendations.Add(new RecommendedKeyword("NotInheritable", VBFeaturesResources.Specifies_that_a_class_cannot_be_used_as_a_base_class));
                }

                if (modifierFacts.OverridableSharedOrPartialKeyword.Kind() == SyntaxKind.None)
                {
                    if (!context.IsInterfaceMemberDeclarationKeywordContext &&
                        modifierFacts.OverridesOrShadowsKeyword.Kind() != SyntaxKind.OverridesKeyword)
                    {
                        recommendations.Add(new RecommendedKeyword("Shared", VBFeaturesResources.Specifies_that_one_or_more_declared_programming_elements_are_associated_with_all_instances_of_a_class_or_structure));
                    }

                    if (modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.OverridableMethod))
                    {
                        recommendations.Add(new RecommendedKeyword("MustOverride", VBFeaturesResources.Specifies_that_a_property_or_procedure_is_not_implemented_in_the_class_and_must_be_overridden_in_a_derived_class_before_it_can_be_used));

                        if (modifierFacts.OverridesOrShadowsKeyword.Kind() != SyntaxKind.ShadowsKeyword)
                        {
                            recommendations.Add(new RecommendedKeyword("NotOverridable", VBFeaturesResources.Specifies_that_a_property_or_procedure_cannot_be_overridden_in_a_derived_class));
                        }

                        if (modifierFacts.OverridesOrShadowsKeyword.Kind() != SyntaxKind.OverridesKeyword)
                        {
                            recommendations.Add(new RecommendedKeyword("Overridable", VBFeaturesResources.Specifies_that_a_property_or_procedure_can_be_overridden_by_an_identically_named_property_or_procedure_in_a_derived_class));
                        }
                    }
                }

                if (modifierFacts.OverridesOrShadowsKeyword.Kind() == SyntaxKind.None)
                {
                    if (modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.OverridableKeyword &&
                        modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method | PossibleDeclarationTypes.Property) &&
                        !context.IsInterfaceMemberDeclarationKeywordContext &&
                        modifierFacts.SharedKeyword.Kind() == SyntaxKind.None &&
                        modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.PartialKeyword)
                    {
                        recommendations.Add(new RecommendedKeyword("Overrides", VBFeaturesResources.Specifies_that_a_property_or_procedure_overrides_an_identically_named_property_or_procedure_inherited_from_a_base_class));
                    }

                    if (modifierFacts.OverloadsKeyword.Kind() == SyntaxKind.None &&
                        modifierFacts.OverridableSharedOrPartialKeyword.Kind() != SyntaxKind.NotOverridableKeyword &&
                        modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method | PossibleDeclarationTypes.Property | PossibleDeclarationTypes.Operator))
                    {
                        recommendations.Add(new RecommendedKeyword("Shadows", VBFeaturesResources.Specifies_that_a_declared_programming_element_redeclares_and_hides_an_identically_named_element_in_a_base_class));
                    }
                }

                if (modifierFacts.OverloadsKeyword.Kind() == SyntaxKind.None &&
                    modifierFacts.OverridesOrShadowsKeyword.Kind() != SyntaxKind.ShadowsKeyword &&
                    modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property | PossibleDeclarationTypes.Method | PossibleDeclarationTypes.Operator))
                {
                    recommendations.Add(new RecommendedKeyword("Overloads", VBFeaturesResources.Specifies_that_a_property_or_procedure_re_declares_one_or_more_existing_properties_or_procedures_with_the_same_name));
                }

                if (modifierFacts.DefaultKeyword.Kind() == SyntaxKind.None &&
                    modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) &&
                    modifierFacts.AccessibilityKeyword.Kind() != SyntaxKind.PrivateKeyword)
                {
                    recommendations.Add(new RecommendedKeyword("Default", VBFeaturesResources.Identifies_a_property_as_the_default_property_of_its_class_structure_or_interface));
                }

                if (modifierFacts.NarrowingOrWideningKeyword.Kind() == SyntaxKind.None && modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Operator))
                {
                    recommendations.Add(new RecommendedKeyword("Narrowing", VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_might_not_be_able_to_hold_some_of_the_possible_values_of_the_original_class_or_structure));
                    recommendations.Add(new RecommendedKeyword("Widening", VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_can_hold_all_possible_values_of_the_original_class_or_structure));
                }
            }
        }

        return recommendations.ToImmutableArray();
    }
}
