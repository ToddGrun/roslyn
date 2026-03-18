// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping;

internal sealed class VisualBasicSyntaxWrappingOptions : SyntaxWrappingOptions
{
    public VisualBasicSyntaxWrappingOptions(
        VisualBasicSyntaxFormattingOptions formattingOptions,
        OperatorPlacementWhenWrappingPreference operatorPlacement)
        : base(formattingOptions, operatorPlacement)
    {
    }

    public static VisualBasicSyntaxWrappingOptions Create(IOptionsReader options)
    {
        return new VisualBasicSyntaxWrappingOptions(
            formattingOptions: new VisualBasicSyntaxFormattingOptions(options),
            operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping));
    }
}
