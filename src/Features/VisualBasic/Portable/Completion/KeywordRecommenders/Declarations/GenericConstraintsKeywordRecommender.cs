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

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

internal class GenericConstraintsKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        var recommendations = new List<RecommendedKeyword>();
        recommendations.Add(new RecommendedKeyword("Class", VBFeaturesResources.Constrains_a_generic_type_parameter_to_require_that_any_type_argument_passed_to_it_be_a_reference_type));
        recommendations.Add(new RecommendedKeyword("Structure", VBFeaturesResources.Constrains_a_generic_type_parameter_to_require_that_any_type_argument_passed_to_it_be_a_value_type));
        recommendations.Add(new RecommendedKeyword("New", VBFeaturesResources.Specifies_a_constructor_constraint_on_a_generic_type_parameter));

        if (targetToken.IsChildToken<TypeParameterSingleConstraintClauseSyntax>(constraint => constraint.AsKeyword))
        {
            return recommendations.ToImmutableArray();
        }
        else if (targetToken.Parent is TypeParameterMultipleConstraintClauseSyntax)
        {
            var multipleConstraint = (TypeParameterMultipleConstraintClauseSyntax)targetToken.Parent;
            if (targetToken == multipleConstraint.OpenBraceToken || targetToken.Kind() == SyntaxKind.CommaToken)
            {
                var previousConstraints = multipleConstraint.Constraints.Where(c => c.Span.End < context.Position).ToList();

                // Structure can only be listed with previous type constraints
                if (previousConstraints.Any(constraint => !constraint.IsKind(SyntaxKind.TypeConstraint)))
                {
                    recommendations.RemoveAll(k => k.Keyword == "Structure");
                }

                if (previousConstraints.Any(constraint => constraint.IsKind(SyntaxKind.ClassConstraint, SyntaxKind.StructureConstraint)))
                {
                    recommendations.RemoveAll(k => k.Keyword == "Class");
                }

                if (previousConstraints.Any(constraint => constraint.IsKind(SyntaxKind.NewConstraint, SyntaxKind.StructureConstraint)))
                {
                    recommendations.RemoveAll(k => k.Keyword == "New");
                }

                return recommendations.ToImmutableArray();
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
