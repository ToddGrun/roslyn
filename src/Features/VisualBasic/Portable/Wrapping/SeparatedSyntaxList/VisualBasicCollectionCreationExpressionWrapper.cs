// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList;

internal class VisualBasicCollectionCreationExpressionWrapper
    : AbstractVisualBasicSeparatedSyntaxListWrapper<CollectionInitializerSyntax, ExpressionSyntax>
{
    protected override string Indent_all_items => FeaturesResources.Indent_all_elements;
    protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_elements;
    protected override string Unwrap_list => FeaturesResources.Unwrap_initializer;
    protected override string Wrap_every_item => FeaturesResources.Wrap_initializer;
    protected override string Wrap_long_list => FeaturesResources.Wrap_long_initializer;

    public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => false;
    public override bool Supports_WrapEveryGroup_UnwrapFirst => false;
    public override bool Supports_WrapLongGroup_UnwrapFirst => false;

    // unreachable as we explicitly declare that we don't support these scenarios.

    protected override string Align_wrapped_items => throw ExceptionUtilities.Unreachable();
    protected override string Indent_wrapped_items => throw ExceptionUtilities.Unreachable();
    protected override string Unwrap_and_indent_all_items => throw ExceptionUtilities.Unreachable();

    protected override bool ShouldMoveCloseBraceToNewLine => true;

    protected override SyntaxToken FirstToken(CollectionInitializerSyntax listSyntax)
    {
        return listSyntax.OpenBraceToken;
    }

    protected override SyntaxToken LastToken(CollectionInitializerSyntax listSyntax)
    {
        return listSyntax.CloseBraceToken;
    }

    protected override SeparatedSyntaxList<ExpressionSyntax> GetListItems(CollectionInitializerSyntax listSyntax)
    {
        return listSyntax.Initializers;
    }

    protected override CollectionInitializerSyntax TryGetApplicableList(SyntaxNode node)
    {
        return (node as ArrayCreationExpressionSyntax)?.Initializer
            ?? (node as ObjectCollectionInitializerSyntax)?.Initializer;
    }

    protected override bool PositionIsApplicable(
        SyntaxNode root, int position, SyntaxNode declaration, bool containsSyntaxError, CollectionInitializerSyntax listSyntax)
    {
        if (containsSyntaxError)
        {
            return false;
        }

        return listSyntax.Span.Contains(position);
    }
}
