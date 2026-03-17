// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Implements" keyword
/// </summary>
internal class ImplementsKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;

        var typeBlock = targetToken.GetAncestor<TypeBlockSyntax>();
        if (typeBlock is InterfaceBlockSyntax)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        if (context.IsAfterStatementOfKind(
                SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.ImplementsStatement, SyntaxKind.InheritsStatement))
        {
            return ImmutableArray.Create(new RecommendedKeyword("Implements", VBFeaturesResources.Specifies_one_or_more_interfaces_or_interface_members_that_must_be_implemented_in_the_class_or_structure_definition_in_which_the_Implements_statement_appears));
        }

        if (context.IsFollowingParameterListOrAsClauseOfMethodDeclaration() ||
            context.IsFollowingCompletePropertyDeclaration(cancellationToken) ||
            context.IsFollowingCompleteEventDeclaration())
        {
            if (typeBlock != null)
            {
                // We need to check to see if any of the partial types parts declare an implements statement.
                // If not, we don't show the Implements keyword.
                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeBlock, cancellationToken);
                if (typeSymbol != null)
                {
                    foreach (var reference in typeSymbol.DeclaringSyntaxReferences)
                    {
                        var typeStatement = reference.GetSyntax(cancellationToken) as TypeStatementSyntax;

                        if (typeStatement != null &&
                            typeStatement.Parent is TypeBlockSyntax &&
                            ((TypeBlockSyntax)typeStatement.Parent).Implements.Count > 0)
                        {
                            return ImmutableArray.Create(new RecommendedKeyword("Implements", VBFeaturesResources.Indicates_that_a_class_or_structure_member_is_providing_the_implementation_for_a_member_defined_in_an_interface));
                        }
                    }
                }
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
