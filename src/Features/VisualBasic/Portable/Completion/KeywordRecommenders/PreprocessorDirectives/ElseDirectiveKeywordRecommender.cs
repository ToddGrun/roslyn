// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#Else" preprocessor directive
/// </summary>
internal class ElseDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("#Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_SharpIf_statement_that_is_compiled_if_no_previous_condition_evaluates_to_True)];

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
