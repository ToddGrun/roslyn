// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements;

/// <summary>
/// Recommends the "Exit" keyword at the start of a statement
/// </summary>
internal class ExitKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Exit", VBFeaturesResources.Exits_a_procedure_or_block_and_transfers_execution_immediately_to_the_statement_following_the_procedure_call_or_block_definition_Exit_Do_For_Function_Property_Select_Sub_Try_While)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // Make sure we're in an exitable block
        if (!context.IsInStatementBlockOfKind(
            SyntaxKind.SimpleDoLoopBlock,
            SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
            SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock,
            SyntaxKind.ForBlock, SyntaxKind.ForEachBlock,
            SyntaxKind.FunctionBlock,
            SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression,
            SyntaxKind.PropertyBlock,
            SyntaxKind.SelectBlock,
            SyntaxKind.SubBlock,
            SyntaxKind.TryBlock, SyntaxKind.CatchBlock,
            SyntaxKind.WhileBlock))
        {
            return [];
        }

        if (context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return [];
        }

        // We know that in any executable statement context, there always must be at least one thing we can exit: the
        // function or sub itself (except for Finally blocks)
        return context.IsStatementContext ? s_keywords : [];
    }
}
