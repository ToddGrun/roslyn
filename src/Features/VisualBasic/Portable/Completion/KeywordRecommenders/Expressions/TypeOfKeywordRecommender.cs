// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

/// <summary>
/// Recommends the "TypeOf" keyword.
/// </summary>
internal class TypeOfKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("TypeOf", VBFeaturesResources.Determines_the_run_time_type_of_an_object_reference_variable_and_compares_it_to_a_data_type_Returns_True_or_False_depending_on_whether_the_two_types_are_compatible_result_TypeOf_objectExpression_Is_typeName));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsAnyExpressionContext ? s_keywords : ImmutableArray<RecommendedKeyword>.Empty;
    }
}
