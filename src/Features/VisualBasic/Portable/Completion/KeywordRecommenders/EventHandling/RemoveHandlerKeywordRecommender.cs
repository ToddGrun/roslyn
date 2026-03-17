// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling;

/// <summary>
/// Recommends the "RemoveHandler" keyword.
/// </summary>
internal class RemoveHandlerKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords = ImmutableArray.Create(
                CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.RemoveHandlerKeyword,
                                                             VBFeaturesResources.RemoveHandler_statement,
                                                             Glyph.Keyword,
                                                             new RemoveHandlerStatementDocumentation()));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsStatementContext || context.CanDeclareCustomEventAccessor(SyntaxKind.RemoveHandlerAccessorBlock)
            ? s_keywords
            : ImmutableArray<RecommendedKeyword>.Empty;
    }
}
