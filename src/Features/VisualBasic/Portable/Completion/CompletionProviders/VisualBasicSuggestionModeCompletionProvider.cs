// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(VisualBasicSuggestionModeCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(NamedParameterCompletionProvider))]
[Shared]
internal sealed class VisualBasicSuggestionModeCompletionProvider : AbstractSuggestionModeCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualBasicSuggestionModeCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    protected override async Task<CompletionItem?> GetSuggestionModeItemAsync(Document document, int position, TextSpan itemSpan, CompletionTrigger trigger, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var syntaxTree = semanticModel.SyntaxTree;

        // If we're option explicit off, then basically any expression context can have a
        // builder, since it might be an implicit local declaration.
        var targetToken = syntaxTree.GetTargetToken(position, cancellationToken);

        if (semanticModel.OptionExplicit == false && (syntaxTree.IsExpressionContext(position, targetToken, cancellationToken) || syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)))
        {
            return CreateSuggestionModeItem("", "");
        }

        // Builder if we're typing a field
        var description = VBFeaturesResources.Type_a_name_here_to_declare_a_new_field + "\r\n" +
                          VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab;

        if (syntaxTree.IsFieldNameDeclarationContext(position, targetToken, cancellationToken))
        {
            return CreateSuggestionModeItem(VBFeaturesResources.new_field, description);
        }

        if (targetToken.Kind() == SyntaxKind.None || targetToken.FollowsEndOfStatement(position))
        {
            return null;
        }

        // Builder if we're typing a parameter
        if (syntaxTree.IsParameterNameDeclarationContext(position, cancellationToken))
        {
            // Don't provide a builder if only the "Optional" keyword is recommended --
            // it's mandatory in that case!
            var methodDeclaration = targetToken.GetAncestor<MethodBaseSyntax>();
            if (methodDeclaration != null)
            {
                if (targetToken.Kind() == SyntaxKind.CommaToken && targetToken.Parent.Kind() == SyntaxKind.ParameterList)
                {
                    foreach (var parameter in methodDeclaration.ParameterList.Parameters.Where(p => p.FullSpan.End < position))
                    {
                        // A previous parameter was Optional, so the suggested Optional is an offer they can't refuse. No builder.
                        if (parameter.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.OptionalKeyword))
                        {
                            return null;
                        }
                    }
                }
            }

            description = VBFeaturesResources.Type_a_name_here_to_declare_a_parameter_If_no_preceding_keyword_is_used_ByVal_will_be_assumed_and_the_argument_will_be_passed_by_value + "\r\n" +
                          VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab;

            // Otherwise just return a builder. It won't show up unless other modifiers are
            // recommended, which is what we want.
            return CreateSuggestionModeItem(VBFeaturesResources.parameter_name, description);
        }

        // Builder in select clause: after Select, after comma
        if (targetToken.Parent.Kind() == SyntaxKind.SelectClause)
        {
            if (targetToken.IsKind(SyntaxKind.SelectKeyword, SyntaxKind.CommaToken))
            {
                description = VBFeaturesResources.Type_a_new_name_for_the_column_followed_by_Otherwise_the_original_column_name_with_be_used + "\r\n" +
                              VBFeaturesResources.Note_colon_Use_tab_for_automatic_completion_space_completion_is_disabled_to_avoid_interfering_with_a_new_name;

                return CreateSuggestionModeItem(VBFeaturesResources.result_alias, description);
            }
        }

        // Build after For
        if (targetToken.IsKindOrHasMatchingText(SyntaxKind.ForKeyword) &&
            targetToken.Parent.IsKind(SyntaxKind.ForStatement))
        {
            description = VBFeaturesResources.Type_a_new_variable_name + "\r\n" +
                          VBFeaturesResources.Note_colon_Space_and_completion_are_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab;

            return CreateSuggestionModeItem(VBFeaturesResources.new_variable, description);
        }

        // Build after Using
        if (targetToken.IsKindOrHasMatchingText(SyntaxKind.UsingKeyword) &&
            targetToken.Parent.IsKind(SyntaxKind.UsingStatement))
        {
            description = VBFeaturesResources.Type_a_new_variable_name + "\r\n" +
                          VBFeaturesResources.Note_colon_Space_and_completion_are_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab;

            return CreateSuggestionModeItem(VBFeaturesResources.new_resource, description);
        }

        // Builder at Namespace declaration name
        if (syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken))
        {
            description = VBFeaturesResources.Type_a_name_here_to_declare_a_namespace + "\r\n" +
                          VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab;

            return CreateSuggestionModeItem(FeaturesResources.namespace_name, description);
        }

        TypeStatementSyntax? statementSyntax = null;

        // Builder after Partial (Class|Structure|Interface|Module)
        if (syntaxTree.IsPartialTypeDeclarationNameContext(position, cancellationToken, out statementSyntax))
        {
            return statementSyntax.DeclarationKeyword.Kind() switch
            {
                SyntaxKind.ClassKeyword => CreateSuggestionModeItem(
                    FeaturesResources.class_name,
                    VBFeaturesResources.Type_a_name_here_to_declare_a_partial_class + "\r\n" +
                    VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab),

                SyntaxKind.InterfaceKeyword => CreateSuggestionModeItem(
                    FeaturesResources.interface_name,
                    VBFeaturesResources.Type_a_name_here_to_declare_a_partial_interface + "\r\n" +
                    VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab),

                SyntaxKind.StructureKeyword => CreateSuggestionModeItem(
                    VBFeaturesResources.structure_name,
                    VBFeaturesResources.Type_a_name_here_to_declare_a_partial_structure + "\r\n" +
                    VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab),

                SyntaxKind.ModuleKeyword => CreateSuggestionModeItem(
                    VBFeaturesResources.module_name,
                    VBFeaturesResources.Type_a_name_here_to_declare_a_partial_module + "\r\n" +
                    VBFeaturesResources.Note_colon_Space_completion_is_disabled_to_avoid_potential_interference_To_insert_a_name_from_the_list_use_tab),

                _ => null
            };
        }

        return null;
    }
}
