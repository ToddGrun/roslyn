// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling;

/// <summary>
/// Recommends the "Handles" keyword.
/// </summary>
internal class HandlesKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Handles", VBFeaturesResources.Declares_that_a_procedure_handles_a_specified_event));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        if (context.IsFollowingParameterListOrAsClauseOfMethodDeclaration())
        {
            var targetToken = context.TargetToken;
            var typeBlock = targetToken.GetAncestor<TypeBlockSyntax>();

            if (typeBlock == null || !typeBlock.IsKind(SyntaxKind.ClassBlock, SyntaxKind.ModuleBlock))
            {
                return ImmutableArray<RecommendedKeyword>.Empty;
            }

            var methodDeclaration = targetToken.GetAncestor<MethodStatementSyntax>();
            if (methodDeclaration == null || methodDeclaration.Modifiers.Any(SyntaxKind.IteratorKeyword))
            {
                return ImmutableArray<RecommendedKeyword>.Empty;
            }

            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
