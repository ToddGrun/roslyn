// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the "Group" query operator.
/// </summary>
internal class GroupKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsQueryOperatorContext)
        {
            return [new RecommendedKeyword("Group", VBFeaturesResources.Groups_elements_that_have_a_common_key)];
        }

        var targetToken = context.TargetToken;

        // Group By ... Into |
        // Group Join ... Into |
        if (targetToken.IsChildToken<GroupByClauseSyntax>(static g => g.IntoKeyword) ||
           targetToken.IsChildToken<GroupJoinClauseSyntax>(static gj => gj.IntoKeyword))
        {
            return [new RecommendedKeyword("Group", VBFeaturesResources.Use_Group_to_specify_that_a_group_named_Group_should_be_created)];
        }

        // Group By ... Into ... = |
        // Group Join ... Into ... = |
        if (targetToken.IsChildToken<VariableNameEqualsSyntax>(static vne => vne.EqualsToken))
        {
            var variableNameEquals = targetToken.GetAncestor<VariableNameEqualsSyntax>();
            if (variableNameEquals.IsParentKind(SyntaxKind.AggregationRangeVariable) &&
              (variableNameEquals.Parent.IsParentKind(SyntaxKind.GroupByClause) ||
               variableNameEquals.Parent.IsParentKind(SyntaxKind.GroupJoinClause)))
            {
                return
                [
                    new RecommendedKeyword("Group",
                        string.Format(VBFeaturesResources.Use_Group_to_specify_that_a_group_named_0_should_be_created,
                            variableNameEquals.Identifier.Identifier.ValueText))
                ];
            }
        }

        // Group By ... Into ... , |
        // Group Join ... Into ... , |
        if (targetToken.IsChildSeparatorToken<GroupByClauseSyntax, AggregationRangeVariableSyntax>(static g => g.AggregationVariables) ||
           targetToken.IsChildSeparatorToken<GroupByClauseSyntax, AggregationRangeVariableSyntax>(static g => g.AggregationVariables))
        {
            return [new RecommendedKeyword("Group", VBFeaturesResources.Groups_elements_that_have_a_common_key)];
        }

        return [];
    }
}
