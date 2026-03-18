// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal static class VisualBasicOutliningHelpers
{
    public const string Ellipsis = "...";
    public const string SpaceEllipsis = " " + Ellipsis;
    public const int MaxXmlDocCommentBannerLength = 120;

    private static string GetNodeBannerText(SyntaxNode node)
    {
        return node.ConvertToSingleLine().ToString() + SpaceEllipsis;
    }

    private static string GetCommentBannerText(SyntaxTrivia comment)
    {
        return "' " + comment.ToString().Substring(1).Trim() + SpaceEllipsis;
    }

    private static BlockSpan? CreateCommentsRegion(SyntaxTrivia startComment, SyntaxTrivia endComment)
    {
        var span = TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End);
        return CreateBlockSpan(
            span, span,
            GetCommentBannerText(startComment),
            autoCollapse: true,
            type: BlockTypes.Comment,
            isCollapsible: true,
            isDefaultCollapsed: false);
    }

    internal static ImmutableArray<BlockSpan> CreateCommentsRegions(SyntaxTriviaList triviaList)
    {
        var spans = ArrayBuilder<BlockSpan>.GetInstance();
        CollectCommentsRegions(triviaList, spans);
        return spans.ToImmutableAndFree();
    }

    internal static void CollectCommentsRegions(SyntaxTriviaList triviaList, ArrayBuilder<BlockSpan> spans)
    {
        if (triviaList.Count > 0)
        {
            SyntaxTrivia? startComment = null;
            SyntaxTrivia? endComment = null;

            foreach (var trivia in triviaList)
            {
                if (trivia.Kind() == SyntaxKind.CommentTrivia)
                {
                    startComment ??= trivia;
                    endComment = trivia;
                }
                else if (trivia.Kind() != SyntaxKind.WhitespaceTrivia &&
                    trivia.Kind() != SyntaxKind.EndOfLineTrivia &&
                    trivia.Kind() != SyntaxKind.EndOfFileToken)
                {
                    if (startComment != null)
                    {
                        spans.AddIfNotNull(CreateCommentsRegion(startComment.Value, endComment.Value));
                        startComment = null;
                        endComment = null;
                    }
                }
            }

            if (startComment != null)
            {
                spans.AddIfNotNull(CreateCommentsRegion(startComment.Value, endComment.Value));
            }
        }
    }

    internal static void CollectCommentsRegions(SyntaxNode node, ArrayBuilder<BlockSpan> spans, BlockStructureOptions options)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (options.IsMetadataAsSource && TryGetLeadingCollapsibleSpan(node, out var span))
        {
            spans.Add(span);
        }
        else
        {
            var triviaList = node.GetLeadingTrivia();
            CollectCommentsRegions(triviaList, spans);
        }
    }

    private static bool TryGetLeadingCollapsibleSpan(SyntaxNode node, [NotNullWhen(true)] out BlockSpan? span)
    {
        var startToken = node.GetFirstToken();
        var endToken = GetEndToken(node);
        if (startToken.IsKind(SyntaxKind.None) || endToken.IsKind(SyntaxKind.None))
        {
            span = null;
            return false;
        }

        var firstComment = startToken.LeadingTrivia.FirstOrNull(t => t.Kind() == SyntaxKind.CommentTrivia);

        var startPosition = firstComment.HasValue
            ? firstComment.Value.SpanStart
            : startToken.SpanStart;

        var endPosition = endToken.SpanStart;

        if (startPosition != endPosition)
        {
            var hintTextEndToken = GetHintTextEndToken(node);
            span = new BlockSpan(
                isCollapsible: true,
                type: BlockTypes.Comment,
                textSpan: TextSpan.FromBounds(startPosition, endPosition),
                hintSpan: TextSpan.FromBounds(startPosition, hintTextEndToken.Span.End),
                bannerText: Ellipsis,
                autoCollapse: true);
            return true;
        }

        span = null;
        return false;
    }

    private static SyntaxToken GetEndToken(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.SubNewStatement))
        {
            var subNewStatement = (SubNewStatementSyntax)node;
            return subNewStatement.Modifiers.FirstOrNull() ?? subNewStatement.DeclarationKeyword;
        }
        else if (node.IsKind(SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement))
        {
            var delegateStatement = (DelegateStatementSyntax)node;
            return delegateStatement.Modifiers.FirstOrNull() ?? delegateStatement.DelegateKeyword;
        }
        else if (node.IsKind(SyntaxKind.EnumStatement))
        {
            var enumStatement = (EnumStatementSyntax)node;
            return enumStatement.Modifiers.FirstOrNull() ?? enumStatement.EnumKeyword;
        }
        else if (node.IsKind(SyntaxKind.EnumMemberDeclaration))
        {
            var enumMemberDeclaration = (EnumMemberDeclarationSyntax)node;
            return enumMemberDeclaration.Identifier;
        }
        else if (node.IsKind(SyntaxKind.EventStatement))
        {
            var eventStatement = (EventStatementSyntax)node;
            return eventStatement.Modifiers.FirstOrNull()
                ?? (eventStatement.CustomKeyword.IsKind(SyntaxKind.None) ? eventStatement.DeclarationKeyword : eventStatement.CustomKeyword);
        }
        else if (node.IsKind(SyntaxKind.FieldDeclaration))
        {
            var fieldDeclaration = (FieldDeclarationSyntax)node;
            return fieldDeclaration.Modifiers.FirstOrNull() ?? fieldDeclaration.Declarators.First().GetFirstToken();
        }
        else if (node.IsKind(SyntaxKind.SubStatement, SyntaxKind.FunctionStatement))
        {
            var methodStatement = (MethodStatementSyntax)node;
            return methodStatement.Modifiers.FirstOrNull() ?? methodStatement.DeclarationKeyword;
        }
        else if (node.IsKind(SyntaxKind.OperatorStatement))
        {
            var operatorStatement = (OperatorStatementSyntax)node;
            return operatorStatement.Modifiers.FirstOrNull() ?? operatorStatement.DeclarationKeyword;
        }
        else if (node.IsKind(SyntaxKind.PropertyStatement))
        {
            var propertyStatement = (PropertyStatementSyntax)node;
            return propertyStatement.Modifiers.FirstOrNull() ?? propertyStatement.DeclarationKeyword;
        }
        else if (node.IsKind(SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.InterfaceStatement, SyntaxKind.ModuleStatement))
        {
            var typeStatement = (TypeStatementSyntax)node;
            return typeStatement.Modifiers.FirstOrNull() ?? typeStatement.DeclarationKeyword;
        }
        else
        {
            return default;
        }
    }

    private static SyntaxToken GetHintTextEndToken(SyntaxNode node)
    {
        return node.GetLastToken();
    }

    internal static BlockSpan? CreateBlockSpan(
        TextSpan span,
        TextSpan hintSpan,
        string bannerText,
        bool autoCollapse,
        string type,
        bool isCollapsible,
        bool isDefaultCollapsed)
    {
        return new BlockSpan(
            textSpan: span,
            hintSpan: hintSpan,
            bannerText: bannerText,
            autoCollapse: autoCollapse,
            isDefaultCollapsed: isDefaultCollapsed,
            type: type,
            isCollapsible: isCollapsible);
    }

    internal static BlockSpan? CreateBlockSpanFromBlock(
        SyntaxNode blockNode,
        string bannerText,
        bool autoCollapse,
        string type,
        bool isCollapsible)
    {
        return CreateBlockSpan(
            blockNode.Span, GetHintSpan(blockNode),
            bannerText, autoCollapse,
            type, isCollapsible, isDefaultCollapsed: false);
    }

    internal static BlockSpan? CreateBlockSpanFromBlock(
        SyntaxNode blockNode,
        SyntaxNode bannerNode,
        bool autoCollapse,
        string type,
        bool isCollapsible)
    {
        return CreateBlockSpan(
            blockNode.Span, GetHintSpan(blockNode),
            GetNodeBannerText(bannerNode),
            autoCollapse, type, isCollapsible, isDefaultCollapsed: false);
    }

    private static TextSpan GetHintSpan(SyntaxNode blockNode)
    {
        var firstToken = blockNode.GetFirstToken();
        if (firstToken.Kind() == SyntaxKind.LessThanToken &&
           firstToken.Parent.IsKind(SyntaxKind.AttributeList))
        {
            var attributeOwner = firstToken.Parent.Parent;
            foreach (var child in attributeOwner.ChildNodesAndTokens())
            {
                if (child.Kind() != SyntaxKind.AttributeList)
                {
                    return TextSpan.FromBounds(child.SpanStart, blockNode.Span.End);
                }
            }
        }

        return blockNode.Span;
    }
}
