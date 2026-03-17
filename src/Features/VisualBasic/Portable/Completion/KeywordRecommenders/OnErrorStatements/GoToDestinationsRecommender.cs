// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements;

/// <summary>
/// Recommends 0 and -1 as the "destinations" of where to go to after On Error Goto
/// </summary>
internal class GoToDestinationsRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("0"), new RecommendedKeyword("-1")];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        return targetToken.Kind == SyntaxKind.GoToKeyword && targetToken.Parent is OnErrorGoToStatementSyntax
            ? s_keywords
            : [];
    }
}
