// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddFileBanner;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
using Microsoft.CodeAnalysis.VisualBasic.FileHeaders;

namespace Microsoft.CodeAnalysis.VisualBasic.AddFileBanner;

[ExportNewDocumentFormattingProvider(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicAddFileBannerNewDocumentFormattingProvider()
    : AbstractAddFileBannerNewDocumentFormattingProvider
{
    protected override SyntaxGenerator SyntaxGenerator => VisualBasicSyntaxGenerator.Instance;
    protected override SyntaxGeneratorInternal SyntaxGeneratorInternal => VisualBasicSyntaxGeneratorInternal.Instance;

    protected override AbstractFileHeaderHelper FileHeaderHelper => VisualBasicFileHeaderHelper.Instance;
}
