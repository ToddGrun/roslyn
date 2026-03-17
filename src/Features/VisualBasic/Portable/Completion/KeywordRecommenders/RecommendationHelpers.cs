// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders;

internal static class RecommendationHelpers
{
    internal static bool IsOnErrorStatement(SyntaxNode node)
    {
        return node is OnErrorGoToStatementSyntax or OnErrorResumeNextStatementSyntax;
    }

    /// <summary>
    /// Returns the parent of the node given. node may be null, which will cause this function to return null.
    /// </summary>
    internal static SyntaxNode GetParentOrNull(this SyntaxNode node)
    {
        return node?.Parent;
    }

    internal static bool IsFollowingCompleteAsNewClause(this SyntaxToken token)
    {
        var asNewClause = token.GetAncestor<AsNewClauseSyntax>();
        if (asNewClause == null)
        {
            return false;
        }

        SyntaxToken lastToken;
        switch (asNewClause.NewExpression.Kind())
        {
            case SyntaxKind.ObjectCreationExpression:
                var objectCreation = (ObjectCreationExpressionSyntax)asNewClause.NewExpression;
                lastToken = objectCreation.ArgumentList != null
                    ? objectCreation.ArgumentList.CloseParenToken
                    : asNewClause.Type.GetLastToken(includeZeroWidth: true);
                break;
            case SyntaxKind.AnonymousObjectCreationExpression:
                var anonymousObjectCreation = (AnonymousObjectCreationExpressionSyntax)asNewClause.NewExpression;
                lastToken = anonymousObjectCreation.Initializer != null
                    ? anonymousObjectCreation.Initializer.CloseBraceToken
                    : asNewClause.Type.GetLastToken(includeZeroWidth: true);
                break;
            case SyntaxKind.ArrayCreationExpression:
                var arrayCreation = (ArrayCreationExpressionSyntax)asNewClause.NewExpression;
                lastToken = arrayCreation.Initializer != null
                    ? arrayCreation.Initializer.CloseBraceToken
                    : asNewClause.Type.GetLastToken(includeZeroWidth: true);
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(asNewClause.NewExpression.Kind());
        }

        return token == lastToken;
    }

    private static bool IsLastTokenOfObjectCreation(this SyntaxToken token, ObjectCreationExpressionSyntax objectCreation)
    {
        if (objectCreation == null)
        {
            return false;
        }

        var lastToken = objectCreation.ArgumentList != null
            ? objectCreation.ArgumentList.CloseParenToken
            : objectCreation.Type.GetLastToken(includeZeroWidth: true);

        return token == lastToken;
    }

    internal static bool IsFollowingCompleteObjectCreationInitializer(this SyntaxToken token)
    {
        var variableDeclarator = token.GetAncestor<VariableDeclaratorSyntax>();
        if (variableDeclarator == null)
        {
            return false;
        }

        var objectCreation = token.GetAncestors<ObjectCreationExpressionSyntax>()
            .Where(oc => oc.Parent != null &&
                        oc.Parent.Kind() != SyntaxKind.AsNewClause &&
                        variableDeclarator.Initializer != null &&
                        variableDeclarator.Initializer.Value == oc)
            .FirstOrDefault();

        return token.IsLastTokenOfObjectCreation(objectCreation);
    }

    internal static bool IsFollowingCompleteObjectCreation(this SyntaxToken token)
    {
        var objectCreation = token.GetAncestor<ObjectCreationExpressionSyntax>();
        return token.IsLastTokenOfObjectCreation(objectCreation);
    }

    internal static ExpressionSyntax LastJoinKey(this SeparatedSyntaxList<JoinConditionSyntax> collection)
    {
        var lastJoinCondition = collection.LastOrDefault();
        return lastJoinCondition?.Right;
    }

    internal static bool IsFromIdentifierNode(this SyntaxToken token, IdentifierNameSyntax identifierSyntax)
    {
        return identifierSyntax != null &&
            token == identifierSyntax.Identifier &&
            identifierSyntax.Identifier.GetTypeCharacter() == TypeCharacter.None;
    }

    internal static bool IsFromIdentifierNode(this SyntaxToken token, ModifiedIdentifierSyntax identifierSyntax)
    {
        return identifierSyntax != null &&
            token == identifierSyntax.Identifier &&
            identifierSyntax.Identifier.GetTypeCharacter() == TypeCharacter.None;
    }

    internal static bool IsFromIdentifierNode(this SyntaxToken token, SyntaxNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (node is IdentifierNameSyntax identifierName && token.IsFromIdentifierNode(identifierName))
        {
            return true;
        }

        if (node is ModifiedIdentifierSyntax modifiedIdentifierName && token.IsFromIdentifierNode(modifiedIdentifierName))
        {
            return true;
        }

        return false;
    }

    internal static bool IsFromIdentifierNode<TParent>(this SyntaxToken token, Func<TParent, SyntaxNode> identifierNodeSelector)
        where TParent : SyntaxNode
    {
        var ancestor = token.GetAncestor<TParent>();
        if (ancestor == null)
        {
            return false;
        }

        return token.IsFromIdentifierNode(identifierNodeSelector(ancestor));
    }

    internal static RecommendedKeyword CreateRecommendedKeywordForIntrinsicOperator(
        SyntaxKind kind,
        string firstLine,
        Glyph glyph,
        AbstractIntrinsicOperatorDocumentation intrinsicOperator,
        SemanticModel semanticModel = null,
        int position = -1)
    {
        return new RecommendedKeyword(SyntaxFacts.GetText(kind), glyph,
            c =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(firstLine);
                stringBuilder.AppendLine(intrinsicOperator.DocumentationText);

                void AppendParts(IEnumerable<SymbolDisplayPart> parts)
                {
                    foreach (var part in parts)
                    {
                        stringBuilder.Append(part.ToString());
                    }
                }

                AppendParts(intrinsicOperator.PrefixParts);

                for (int i = 0; i < intrinsicOperator.ParameterCount; i++)
                {
                    if (i != 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    AppendParts(intrinsicOperator.GetParameterDisplayParts(i));
                }

                AppendParts(intrinsicOperator.GetSuffix(semanticModel, position, null, c));

                return stringBuilder.ToString().ToSymbolDisplayParts();
            },
            isIntrinsic: true);
    }
}
