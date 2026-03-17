// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#End If" preprocessor directive
/// </summary>
internal class EndIfDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreprocessorEndDirectiveKeywordContext && HasMatchingIfDirective(context, cancellationToken))
        {
            return [new RecommendedKeyword("If", VBFeaturesResources.Terminates_the_definition_of_an_SharpIf_block)];
        }

        if (context.IsPreprocessorStartContext)
        {
            var innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken);

            if (innermostKind.HasValue)
            {
                return [new RecommendedKeyword("#End If", VBFeaturesResources.Terminates_the_definition_of_an_SharpIf_block)];
            }
        }

        return [];
    }

    private static bool HasMatchingIfDirective(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken);

        return innermostKind.HasValue;
    }
}
