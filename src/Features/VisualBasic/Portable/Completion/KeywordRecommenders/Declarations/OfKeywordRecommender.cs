// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

internal class OfKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (!targetToken.IsKind(SyntaxKind.OpenParenToken))
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var methodDeclaration = targetToken.GetAncestor<MethodStatementSyntax>();
        if (methodDeclaration != null)
        {
            if (methodDeclaration.TypeParameterList != null)
            {
                if (targetToken == methodDeclaration.TypeParameterList.OpenParenToken)
                {
                    return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
                }
            }
            else if (methodDeclaration.ParameterList != null)
            {
                // If we don't have a TypeParametersOpt, then we might be in a place where it's ambiguous where we are.
                // For example, typing Sub Goo(|, it's not clear if that's a TypeParameters block I'm in or a regular
                // block. The parser chooses the sane choice of calling that a regular parameters block until it knows
                // otherwise.
                if (targetToken == methodDeclaration.ParameterList.OpenParenToken)
                {
                    return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
                }
            }
        }

        var implementsClause = targetToken.GetAncestor<ImplementsClauseSyntax>();
        if (implementsClause != null)
        {
            if (targetToken.IsKind(SyntaxKind.OpenParenToken) && targetToken.Parent.IsKind(SyntaxKind.TypeArgumentList))
            {
                return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
            }
        }

        var inheritsStatement = targetToken.GetAncestor<InheritsStatementSyntax>();
        if (inheritsStatement != null)
        {
            if (targetToken.IsKind(SyntaxKind.OpenParenToken) && targetToken.Parent.IsKind(SyntaxKind.TypeArgumentList))
            {
                return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
            }
        }

        var delegateDeclaration = targetToken.GetAncestor<DelegateStatementSyntax>();
        if (delegateDeclaration != null)
        {
            if (delegateDeclaration.TypeParameterList != null)
            {
                if (targetToken == delegateDeclaration.TypeParameterList.OpenParenToken)
                {
                    return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
                }
            }
            else if (delegateDeclaration.ParameterList != null)
            {
                if (targetToken == delegateDeclaration.ParameterList.OpenParenToken)
                {
                    return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
                }
            }
        }

        var typeDeclaration = targetToken.GetAncestor<TypeStatementSyntax>();
        if (typeDeclaration != null && typeDeclaration.IsKind(SyntaxKind.ClassStatement, SyntaxKind.InterfaceStatement, SyntaxKind.StructureStatement))
        {
            if (typeDeclaration.TypeParameterList != null)
            {
                if (targetToken == typeDeclaration.TypeParameterList.OpenParenToken)
                {
                    return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
                }
            }
        }

        // Cases:
        //  Dim f As New Goo(|
        if (targetToken.Parent.IsKind(SyntaxKind.ArgumentList) && targetToken.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
        {
            return ImmutableArray.Create(new RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
