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
/// Recommends the "Function" keyword in member declaration contexts
/// </summary>
internal class FunctionKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Function", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Function_procedure_that_is_a_procedure_that_returns_a_value_to_the_calling_code));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken CancellationToken)
    {
        if (context.IsTypeMemberDeclarationKeywordContext || context.IsInterfaceMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.OverridableSharedOrPartialKeyword.Kind() == SyntaxKind.PartialKeyword)
            {
                return ImmutableArray<RecommendedKeyword>.Empty;
            }

            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Method | PossibleDeclarationTypes.IteratorFunction))
            {
                return s_keywords;
            }
        }

        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) &&
            context.IsInStatementBlockOfKind(SyntaxKind.FunctionBlock, SyntaxKind.MultiLineFunctionLambdaExpression) &&
            !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
