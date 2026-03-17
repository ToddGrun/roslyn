// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

internal static class PreprocessorHelpers
{
    public static SyntaxKind? GetInnermostIfPreprocessorKind(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var kindStack = new IfDirectiveVisitor(syntaxTree, position, cancellationToken).GetStack();

        if (kindStack.Count == 0)
        {
            return null;
        }
        else
        {
            return kindStack.Peek();
        }
    }

    private class IfDirectiveVisitor : VisualBasicSyntaxVisitor
    {
        private readonly Stack<SyntaxKind> _kindStack = new();
        private readonly int _maxPosition;

        public IfDirectiveVisitor(SyntaxTree syntaxTree, int maxPosition, CancellationToken cancellationToken)
        {
            _maxPosition = maxPosition;
            Visit(syntaxTree.GetRoot(cancellationToken));
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            if (!node.ContainsDirectives)
            {
                return;
            }

            foreach (var leadingTrivia in node.GetLeadingTrivia())
            {
                if (leadingTrivia.HasStructure)
                {
                    Visit(leadingTrivia.GetStructure());
                }
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.FullSpan.Start > _maxPosition)
                {
                    break;
                }

                if (child.IsNode)
                {
                    Visit(child.AsNode());
                }
            }

            foreach (var followingTrivia in node.GetTrailingTrivia())
            {
                if (followingTrivia.HasStructure)
                {
                    Visit(followingTrivia.GetStructure());
                }
            }
        }

        public override void VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
        {
            if (node.Kind == SyntaxKind.IfDirectiveTrivia)
            {
                _kindStack.Push(node.Kind);
            }
            else if (node.Kind == SyntaxKind.ElseIfDirectiveTrivia)
            {
                // If we're closing our previous context, then pop that one
                if (_kindStack.Count > 0)
                {
                    _kindStack.Pop();
                }

                _kindStack.Push(node.Kind);
            }
        }

        public override void VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
        {
            if (_kindStack.Count > 0)
            {
                _kindStack.Pop();
            }

            _kindStack.Push(node.Kind);
        }

        public override void VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
        {
            if (_kindStack.Count > 0)
            {
                _kindStack.Pop();
            }
        }

        public Stack<SyntaxKind> GetStack()
            => _kindStack;
    }
}
