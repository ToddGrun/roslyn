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
/// Recommends the "Dim" keyword in all appropriate contexts.
/// </summary>
internal class DimKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Dim", VBFeaturesResources.Declares_and_allocates_storage_space_for_one_or_more_variables_Dim_var_bracket_As_bracket_New_bracket_dataType_bracket_boundList_bracket_bracket_bracket_initializer_bracket_bracket_var2_bracket));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken CancellationToken)
    {
        // It can start a statement
        if (context.IsMultiLineStatementContext)
        {
            return s_keywords;
        }

        if (context.IsTypeMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;

            // In Dev10, we don't show it after Const (but will after ReadOnly, even though the formatter removes it)
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Field) &&
                modifiers.MutabilityOrWithEventsKeyword.Kind() != SyntaxKind.ConstKeyword &&
                modifiers.DimKeyword.Kind() == SyntaxKind.None)
            {
                return s_keywords;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
