// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "As" keyword in all types of declarations.
/// </summary>
internal class AsKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("As", VBFeaturesResources.Specifies_a_data_type_in_a_declaration_statement));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        var asKeyword = s_keywords;

        // Query: Aggregate x |
        // Query: Group Join x |
        if (targetToken.IsFromIdentifierNode<CollectionRangeVariableSyntax>(collectionRange => collectionRange.Identifier))
        {
            return asKeyword;
        }

        // Query: Let x |
        var expressionRangeVariable = targetToken.GetAncestor<ExpressionRangeVariableSyntax>();
        if (expressionRangeVariable != null && expressionRangeVariable.NameEquals != null)
        {
            if (targetToken.IsFromIdentifierNode(expressionRangeVariable.NameEquals.Identifier))
            {
                return asKeyword;
            }
        }

        // For x |
        if (targetToken.IsFromIdentifierNode<ForStatementSyntax>(forStatement => forStatement.ControlVariable))
        {
            return asKeyword;
        }

        // For Each x |
        if (targetToken.IsFromIdentifierNode<ForEachStatementSyntax>(forEachStatement => forEachStatement.ControlVariable))
        {
            return asKeyword;
        }

        // All parameter types. In this case we have to drill into the parameter to make sure untyped
        if (targetToken.IsFromIdentifierNode<ParameterSyntax>(parameter => parameter.Identifier))
        {
            return asKeyword;
        }

        // Sub Goo(Of T |
        if (targetToken.IsChildToken<TypeParameterSyntax>(typeParameter => typeParameter.Identifier))
        {
            return asKeyword;
        }

        // Enum Goo |
        if (targetToken.IsChildToken<EnumStatementSyntax>(enumDeclaration => enumDeclaration.Identifier))
        {
            return asKeyword;
        }

        // Catch goo
        if (targetToken.IsFromIdentifierNode<CatchStatementSyntax>(catchStatement => catchStatement.IdentifierName))
        {
            return asKeyword;
        }

        // Function x() |
        // Operator x() |
        if (targetToken.IsChildToken<ParameterListSyntax>(paramList => paramList.CloseParenToken))
        {
            var methodDeclaration = targetToken.GetAncestor<MethodBaseSyntax>();
            if (methodDeclaration.IsKind(SyntaxKind.FunctionStatement, SyntaxKind.OperatorStatement,
                                          SyntaxKind.DeclareFunctionStatement, SyntaxKind.DelegateFunctionStatement,
                                          SyntaxKind.PropertyStatement, SyntaxKind.FunctionLambdaHeader))
            {
                return asKeyword;
            }
        }

        // Function Goo |
        if (targetToken.IsChildToken<MethodStatementSyntax>(functionDeclaration => functionDeclaration.Identifier) &&
            !targetToken.GetAncestor<MethodBaseSyntax>().IsKind(SyntaxKind.SubStatement))
        {
            return asKeyword;
        }

        // Property Goo |
        if (targetToken.IsChildToken<PropertyStatementSyntax>(propertyDeclaration => propertyDeclaration.Identifier))
        {
            return asKeyword;
        }

        // Custom Event Goo |
        if (targetToken.IsChildToken<EventStatementSyntax>(eventDeclaration => eventDeclaration.Identifier))
        {
            return asKeyword;
        }

        // Using goo |
        var usingStatement = targetToken.GetAncestor<UsingStatementSyntax>();
        if (usingStatement != null && usingStatement.Expression != null && !usingStatement.Expression.IsMissing)
        {
            if (usingStatement.Expression == targetToken.Parent)
            {
                return asKeyword;
            }
        }

        // Public Async |
        // Public Iterator |
        // but not...
        // Async |
        // Iterator |
        if (context.IsTypeMemberDeclarationKeywordContext &&
            (targetToken.HasMatchingText(SyntaxKind.AsyncKeyword) || targetToken.HasMatchingText(SyntaxKind.IteratorKeyword)))
        {
            return asKeyword;
        }

        // Dim x |
        var variableDeclarator = targetToken.GetAncestor<VariableDeclaratorSyntax>();
        if (variableDeclarator != null)
        {
            if (variableDeclarator.Names.Any(name => name.Identifier == targetToken && name.Identifier.GetTypeCharacter() == TypeCharacter.None))
            {
                return asKeyword;
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
