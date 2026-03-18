// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList;

internal partial class VisualBasicArgumentWrapper
    : AbstractVisualBasicSeparatedSyntaxListWrapper<ArgumentListSyntax, ArgumentSyntax>
{
    protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_arguments;
    protected override string Indent_all_items => FeaturesResources.Indent_all_arguments;
    protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_arguments;
    protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_arguments;
    protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_arguments;
    protected override string Unwrap_list => FeaturesResources.Unwrap_argument_list;
    protected override string Wrap_every_item => FeaturesResources.Wrap_every_argument;
    protected override string Wrap_long_list => FeaturesResources.Wrap_long_argument_list;

    public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => true;
    public override bool Supports_WrapEveryGroup_UnwrapFirst => true;
    public override bool Supports_WrapLongGroup_UnwrapFirst => true;

    protected override bool ShouldMoveCloseBraceToNewLine => false;

    protected override SyntaxToken FirstToken(ArgumentListSyntax listSyntax)
    {
        return listSyntax.OpenParenToken;
    }

    protected override SyntaxToken LastToken(ArgumentListSyntax listSyntax)
    {
        return listSyntax.CloseParenToken;
    }

    protected override SeparatedSyntaxList<ArgumentSyntax> GetListItems(ArgumentListSyntax listSyntax)
    {
        return listSyntax.Arguments;
    }

    protected override ArgumentListSyntax TryGetApplicableList(SyntaxNode node)
    {
        return (node as InvocationExpressionSyntax)?.ArgumentList
            ?? (node as ObjectCreationExpressionSyntax)?.ArgumentList;
    }

    protected override bool PositionIsApplicable(
        SyntaxNode root, int position, SyntaxNode declaration, bool containsSyntaxError, ArgumentListSyntax listSyntax)
    {
        if (containsSyntaxError)
        {
            return false;
        }

        var startToken = listSyntax.GetFirstToken();

        // If we have something like  Foo(...)  or  this.Foo(...)  allow anywhere in the Foo(...)
        if (declaration is InvocationExpressionSyntax invocation)
        {
            var expr = invocation.Expression;
            var name = expr as NameSyntax
                ?? (expr as MemberAccessExpressionSyntax)?.Name;

            startToken = name is null ? listSyntax.GetFirstToken() : name.GetFirstToken();
        }
        else if (declaration is ObjectCreationExpressionSyntax)
        {
            // allow anywhere in `New Foo(...)`
            startToken = declaration.GetFirstToken();
        }

        var endToken = listSyntax.GetLastToken();
        var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
        if (!span.IntersectsWith(position))
        {
            return false;
        }

        // allow anywhere in the arg list, as long we don't end up walking through something
        // complex like a lambda/anonymous function.
        var token = root.FindToken(position);
        if (token.Parent.Ancestors().Contains(listSyntax))
        {
            var current = token.Parent;
            while (current != listSyntax)
            {
                if (VisualBasicSyntaxFacts.Instance.IsAnonymousFunctionExpression(current))
                {
                    return false;
                }

                current = current.Parent;
            }
        }

        return true;
    }
}
