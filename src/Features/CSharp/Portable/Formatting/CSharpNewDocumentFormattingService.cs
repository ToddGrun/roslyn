﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[ExportLanguageService(typeof(INewDocumentFormattingService), LanguageNames.CSharp)]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpNewDocumentFormattingService([ImportMany] IEnumerable<Lazy<INewDocumentFormattingProvider, LanguageMetadata>> providers) : AbstractNewDocumentFormattingService(providers)
{
    protected override string Language => LanguageNames.CSharp;
}
