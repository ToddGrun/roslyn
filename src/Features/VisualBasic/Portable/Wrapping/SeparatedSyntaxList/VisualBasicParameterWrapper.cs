// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList;

internal partial class VisualBasicParameterWrapper
    : AbstractVisualBasicSeparatedSyntaxListWrapper<ParameterListSyntax, ParameterSyntax>
{
    protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_parameters;
    protected override string Indent_all_items => FeaturesResources.Indent_all_parameters;
    protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_parameters;
    protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_parameters;
    protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_parameters;
    protected override string Unwrap_list => FeaturesResources.Unwrap_parameter_list;
    protected override string Wrap_every_item => FeaturesResources.Wrap_every_parameter;
    protected override string Wrap_long_list => FeaturesResources.Wrap_long_parameter_list;

    public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => true;
    public override bool Supports_WrapEveryGroup_UnwrapFirst => true;
    public override bool Supports_WrapLongGroup_UnwrapFirst => true;

    protected override bool ShouldMoveCloseBraceToNewLine => false;

    protected override SyntaxToken FirstToken(ParameterListSyntax listSyntax)
    {
        return listSyntax.OpenParenToken;
    }

    protected override SyntaxToken LastToken(ParameterListSyntax listSyntax)
    {
        return listSyntax.CloseParenToken;
    }

    protected override SeparatedSyntaxList<ParameterSyntax> GetListItems(ParameterListSyntax listSyntax)
    {
        return listSyntax.Parameters;
    }

    protected override ParameterListSyntax TryGetApplicableList(SyntaxNode node)
    {
        return node.GetParameterList();
    }

    protected override bool PositionIsApplicable(
        SyntaxNode root, int position, SyntaxNode declaration,
        bool containsSyntaxError, ParameterListSyntax listSyntax)
    {
        var generator = VisualBasicSyntaxGenerator.Instance;
        var attributes = generator.GetAttributes(declaration);

        // We want to offer this feature in the header of the member.  For now, we consider
        // the header to be the part after the attributes, to the end of the parameter list.
        var firstToken = attributes?.Count > 0
            ? attributes.Last().GetLastToken().GetNextToken()
            : declaration.GetFirstToken();

        var lastToken = listSyntax.GetLastToken();

        var headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
        if (!headerSpan.IntersectsWith(position))
        {
            return false;
        }

        if (containsSyntaxError && ContainsOverlappingSyntaxError(declaration, headerSpan))
        {
            return false;
        }

        return true;
    }
}
