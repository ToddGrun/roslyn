// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements;

/// <summary>
/// Recommends the "On" and "Off" options that come after "Option Infer"
/// </summary>
internal class InferOptionsRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [
            new RecommendedKeyword("On", VBFeaturesResources.Turns_a_compiler_option_on),
            new RecommendedKeyword("Off", VBFeaturesResources.Turns_a_compiler_option_off)
        ];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        return context.TargetToken.IsKind(SyntaxKind.InferKeyword) ? s_keywords : [];
    }
}
