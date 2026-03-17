// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#R" preprocessor directive
/// </summary>
internal class ReferenceDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("#R", VBFeaturesResources.Add_a_metadata_reference_to_specified_assembly_and_all_its_dependencies_e_g_Sharpr_myLib_dll)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
        => context.IsPreprocessorStartContext && context.SyntaxTree.IsScript() && !context.SyntaxTree.IsEnumMemberNameContext(context)
            ? s_keywords
            : [];
}
