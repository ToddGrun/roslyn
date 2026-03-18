// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.VisualBasic.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.VisualBasic.Wrapping.ChainedExpression;
using Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping;

[ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.Wrapping), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class VisualBasicWrappingCodeRefactoringProvider()
    : AbstractWrappingCodeRefactoringProvider(s_wrappers)
{
    private static readonly ImmutableArray<ISyntaxWrapper> s_wrappers =
    [
        new VisualBasicArgumentWrapper(),
        new VisualBasicParameterWrapper(),
        new VisualBasicBinaryExpressionWrapper(),
        new VisualBasicChainedExpressionWrapper(),
        new VisualBasicCollectionCreationExpressionWrapper()
    ];

    protected override SyntaxWrappingOptions GetWrappingOptions(IOptionsReader options)
    {
        return VisualBasicSyntaxWrappingOptions.Create(options);
    }
}
