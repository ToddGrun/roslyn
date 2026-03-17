// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Finally" keyword for the statement context
/// </summary>
internal class FinallyKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Finally", VBFeaturesResources.Introduces_a_statement_block_to_be_run_before_exiting_a_Try_structure)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (!context.IsMultiLineStatementContext)
        {
            return [];
        }

        var targetToken = context.TargetToken;
        var tryBlock = targetToken.GetAncestor<TryBlockSyntax>();

        if (tryBlock is null || tryBlock.FinallyBlock is not null)
        {
            return [];
        }

        // If we're in the Try block, then we simply need to make sure we have no catch blocks, or else a Finally
        // won't be valid here
        if (context.IsInStatementBlockOfKind(SyntaxKind.TryBlock) &&
           !IsInCatchOfTry(targetToken, tryBlock))
        {
            if (tryBlock.CatchBlocks.Count == 0)
            {
                return s_keywords;
            }
        }
        else if (IsInCatchOfTry(targetToken, tryBlock))
        {
            if (TextSpan.FromBounds(tryBlock.CatchBlocks.Last().SpanStart, tryBlock.EndTryStatement.SpanStart).Contains(context.Position))
            {
                return s_keywords;
            }
        }

        return [];
    }

    private static bool IsInCatchOfTry(SyntaxToken targetToken, TryBlockSyntax tryBlock)
    {
        var parent = targetToken.Parent;
        while (parent != tryBlock)
        {
            if (parent.IsKind(SyntaxKind.CatchBlock) && tryBlock.CatchBlocks.Contains((CatchBlockSyntax)parent))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }
}
