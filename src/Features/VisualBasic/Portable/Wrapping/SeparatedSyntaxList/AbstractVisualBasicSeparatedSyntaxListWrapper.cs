// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic.Indentation;
using Microsoft.CodeAnalysis.Wrapping;
using Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList;

internal abstract partial class AbstractVisualBasicSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    : AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    where TListSyntax : SyntaxNode
    where TListItemSyntax : SyntaxNode
{
    protected AbstractVisualBasicSeparatedSyntaxListWrapper()
        : base(VisualBasicIndentationService.WithoutParameterAlignmentInstance)
    {
    }

    // The visual basic language always requires the open brace to be on the same line as the collection
    // being initialized.
    protected sealed override bool ShouldMoveOpenBraceToNewLine(SyntaxWrappingOptions options)
    {
        return false;
    }
}
