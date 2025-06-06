﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

[Export(typeof(IExtractMethodService)), Shared]
[ExportLanguageService(typeof(IExtractMethodService), LanguageNames.CSharp)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpExtractMethodService() : AbstractExtractMethodService<
    StatementSyntax,
    StatementSyntax,
    ExpressionSyntax>
{
    protected override SelectionValidator CreateSelectionValidator(SemanticDocument document, TextSpan textSpan, bool localFunction)
        => new CSharpSelectionValidator(document, textSpan, localFunction);

    protected override MethodExtractor CreateMethodExtractor(SelectionResult selectionResult, ExtractMethodGenerationOptions options, bool localFunction)
        => new CSharpMethodExtractor(selectionResult, options, localFunction);
}
