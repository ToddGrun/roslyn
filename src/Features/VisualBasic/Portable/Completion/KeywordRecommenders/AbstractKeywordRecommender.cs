// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders;

internal abstract class AbstractKeywordRecommender : IKeywordRecommender<VisualBasicSyntaxContext>
{
    public ImmutableArray<RecommendedKeyword> RecommendKeywords(
        int position,
        VisualBasicSyntaxContext context,
        CancellationToken cancellationToken)
    {
        return RecommendKeywords(context, cancellationToken);
    }

    internal ImmutableArray<RecommendedKeyword> RecommendKeywords_Test(VisualBasicSyntaxContext context)
    {
        return RecommendKeywords(context, CancellationToken.None);
    }

    protected abstract ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken);
}
