// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "True" and "False" keywords
/// </summary>
internal class TrueFalseKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var matchPriority = ShouldPreselect(context, cancellationToken) ? CodeAnalysis.Completion.MatchPriority.Preselect : CodeAnalysis.Completion.MatchPriority.Default;

        if (context.IsAnyExpressionContext ||
           context.IsPreProcessorExpressionContext)
        {
            return ImmutableArray.Create(
                new RecommendedKeyword("True", VBFeaturesResources.Represents_a_Boolean_value_that_passes_a_conditional_test, matchPriority: matchPriority),
                new RecommendedKeyword("False", VBFeaturesResources.Represents_a_Boolean_value_that_fails_a_conditional_test, matchPriority: matchPriority));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }

    private static bool ShouldPreselect(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var typeInferenceService = context.Document.GetLanguageService<ITypeInferenceService>();
        Contract.ThrowIfNull(typeInferenceService, nameof(typeInferenceService));

        var types = typeInferenceService.InferTypes(context.SemanticModel, context.Position, cancellationToken);

        return types.Any(t => t.SpecialType == SpecialType.System_Boolean);
    }
}
