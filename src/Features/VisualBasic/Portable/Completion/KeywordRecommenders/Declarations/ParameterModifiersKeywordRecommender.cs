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
/// Recommends the ByVal, ByRef, etc keywords.
/// </summary>
internal class ParameterModifiersKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        var methodDeclaration = targetToken.GetAncestor<MethodBaseSyntax>();
        if (methodDeclaration == null || methodDeclaration.ParameterList == null)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var parameterAlreadyHasByValOrByRef = false;
        if (targetToken.GetAncestor<ParameterSyntax>() != null)
        {
            parameterAlreadyHasByValOrByRef = targetToken.GetAncestor<ParameterSyntax>().Modifiers.Any(m => m.IsKind(SyntaxKind.ByValKeyword, SyntaxKind.ByRefKeyword));
        }

        // Compute some basic properties of what is allowed at all in this context
        var byRefAllowed = methodDeclaration is not AccessorStatementSyntax &&
                           methodDeclaration.Kind() != SyntaxKind.PropertyStatement &&
                           methodDeclaration.Kind() != SyntaxKind.OperatorStatement;

        var optionalAndParamArrayAllowed = methodDeclaration is not DelegateStatementSyntax &&
                                           methodDeclaration is not LambdaHeaderSyntax &&
                                           methodDeclaration is not AccessorStatementSyntax &&
                                           methodDeclaration.Kind() != SyntaxKind.EventStatement &&
                                           methodDeclaration.Kind() != SyntaxKind.OperatorStatement;

        // Compute a simple list of the "standard" recommendations assuming nothing special is going on
        var defaultRecommendations = new System.Collections.Generic.List<RecommendedKeyword>();
        defaultRecommendations.Add(new RecommendedKeyword("ByVal", VBFeaturesResources.Specifies_that_an_argument_is_passed_in_such_a_way_that_the_called_procedure_or_property_cannot_change_the_underlying_value_of_the_argument_in_the_calling_code));

        if (byRefAllowed)
        {
            defaultRecommendations.Add(new RecommendedKeyword("ByRef", VBFeaturesResources.Specifies_that_an_argument_is_passed_in_such_a_way_that_the_called_procedure_can_change_the_underlying_value_of_the_argument_in_the_calling_code));
        }

        if (optionalAndParamArrayAllowed)
        {
            defaultRecommendations.Add(new RecommendedKeyword("Optional", VBFeaturesResources.Specifies_that_a_procedure_argument_can_be_omitted_when_the_procedure_is_called));
            defaultRecommendations.Add(new RecommendedKeyword("ParamArray", VBFeaturesResources.Specifies_that_a_procedure_parameter_takes_an_optional_array_of_elements_of_the_specified_type));
        }

        if (methodDeclaration.ParameterList.OpenParenToken == targetToken)
        {
            return defaultRecommendations.ToImmutableArray();
        }
        else if (targetToken.Kind() == SyntaxKind.CommaToken && targetToken.Parent.Kind() == SyntaxKind.ParameterList)
        {
            // Now we get to look at previous declarations and see what might still be valid
            foreach (var parameter in methodDeclaration.ParameterList.Parameters.Where(p => p.FullSpan.End < context.Position))
            {
                // If a previous one had a ParamArray, then nothing is valid anymore, since the ParamArray must
                // always be the last parameter
                if (parameter.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.ParamArrayKeyword))
                {
                    return ImmutableArray<RecommendedKeyword>.Empty;
                }

                // If a previous one had an Optional, then all following must be optional. Following Dev10 behavior,
                // we recommend just Optional as a first recommendation
                if (parameter.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.OptionalKeyword) && optionalAndParamArrayAllowed)
                {
                    return defaultRecommendations.Where(k => k.Keyword == "Optional").ToImmutableArray();
                }
            }

            // We had no special requirements, so return the default set
            return defaultRecommendations.ToImmutableArray();
        }
        else if (targetToken.Kind() == SyntaxKind.OptionalKeyword && !parameterAlreadyHasByValOrByRef)
        {
            return defaultRecommendations.Where(k => k.Keyword.StartsWith("By", System.StringComparison.Ordinal)).ToImmutableArray();
        }
        else if (targetToken.Kind() == SyntaxKind.ParamArrayKeyword && !parameterAlreadyHasByValOrByRef)
        {
            return defaultRecommendations.Where(k => k.Keyword == "ByVal").ToImmutableArray();
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
