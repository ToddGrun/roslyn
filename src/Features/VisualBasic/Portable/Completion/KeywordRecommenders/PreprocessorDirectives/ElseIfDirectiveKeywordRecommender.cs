// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#ElseIf" preprocessor directive
/// </summary>
internal class ElseIfDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("#ElseIf", VBFeaturesResources.Introduces_a_condition_in_an_SharpIf_statement_that_is_tested_if_the_previous_conditional_test_evaluates_to_False)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreprocessorStartContext || context.IsWithinPreprocessorContext)
        {
            var innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken);

            if (innermostKind.HasValue && innermostKind.Value != SyntaxKind.ElseDirectiveTrivia)
            {
                return s_keywords;
            }
        }

        return [];
    }
}
