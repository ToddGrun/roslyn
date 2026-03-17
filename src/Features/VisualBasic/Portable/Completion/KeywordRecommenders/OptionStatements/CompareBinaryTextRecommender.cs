// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements;

/// <summary>
/// Recommends the "Binary" and "Text" options that come after "Option Compare"
/// </summary>
internal class CompareBinaryTextRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [
            new RecommendedKeyword("Binary", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_strict_binary_sort_order),
            new RecommendedKeyword("Text", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_text_sort_order_that_is_not_case_sensitive)
        ];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        return context.TargetToken.IsKind(SyntaxKind.CompareKeyword) ? s_keywords : [];
    }
}
