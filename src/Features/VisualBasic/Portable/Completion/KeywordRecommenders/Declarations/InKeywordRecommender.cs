// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "In" keyword in all types of declarations.
/// </summary>
internal class InKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        Func<ForEachStatementSyntax, SimpleAsClauseSyntax?> getForEachLoopAsOpt = forEachStatement =>
        {
            // TODO: make this API less ugly in the parser
            var variableDeclarator = forEachStatement.ControlVariable as VariableDeclaratorSyntax;
            if (variableDeclarator != null)
            {
                // TODO: improve this
                return (SimpleAsClauseSyntax)variableDeclarator.AsClause;
            }
            else
            {
                return null;
            }
        };

        // For Each x |
        // TODO: figure out if this is the parse tree not acting correctly here. Why is this a SyntaxNonTerminal?
        if (targetToken.IsFromIdentifierNode<ForEachStatementSyntax>(forEachStatement => forEachStatement.ControlVariable) ||
            IsAfterCompleteAsClause<ForEachStatementSyntax>(context, getForEachLoopAsOpt, cancellationToken))
        {
            return ImmutableArray.Create(new RecommendedKeyword("In", VBFeaturesResources.Specifies_the_group_that_the_loop_variable_in_a_For_Each_statement_is_to_traverse));
        }

        // From element |
        // Group Join element |
        if (targetToken.IsFromIdentifierNode<CollectionRangeVariableSyntax>(rangeVariable => rangeVariable.Identifier) ||
            IsAfterCompleteAsClause<CollectionRangeVariableSyntax>(context, rangeVariable => rangeVariable.AsClause, cancellationToken))
        {
            return ImmutableArray.Create(new RecommendedKeyword("In", VBFeaturesResources.Specifies_the_group_that_the_range_variable_is_to_traverse_in_a_query));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }

    private static bool IsAfterCompleteAsClause<T>(
        VisualBasicSyntaxContext context,
        Func<T, SimpleAsClauseSyntax?> childGetter,
        CancellationToken cancellationToken) where T : SyntaxNode
    {
        var targetToken = context.TargetToken;
        var ancestor = targetToken.GetAncestor<T>();

        if (ancestor != null && childGetter(ancestor) != null)
        {
            return context.SyntaxTree.IsFollowingCompleteExpression<SimpleAsClauseSyntax>(
                context.Position, targetToken, asClause => asClause.Type, cancellationToken);
        }
        else
        {
            return false;
        }
    }
}
