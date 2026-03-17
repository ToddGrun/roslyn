// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#Const" preprocessor directive
/// </summary>
internal class ConstDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("#Const", VBFeaturesResources.Defines_a_conditional_compiler_constant_Conditional_compiler_constants_are_always_private_to_the_file_in_which_they_appear_The_expressions_used_to_initialize_them_can_contain_only_conditional_compiler_constants_and_literals)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsPreprocessorStartContext && !context.SyntaxTree.IsEnumMemberNameContext(context)
            ? s_keywords
            : [];
}
