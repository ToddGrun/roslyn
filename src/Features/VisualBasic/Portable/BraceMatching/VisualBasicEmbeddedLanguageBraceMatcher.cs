// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;

namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching;

[ExportBraceMatcher(LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicEmbeddedLanguageBraceMatcher(
    [ImportMany] IEnumerable<Lazy<IEmbeddedLanguageBraceMatcher, EmbeddedLanguageMetadata>> services)
    : AbstractEmbeddedLanguageBraceMatcher(
        LanguageNames.VisualBasic,
        VisualBasicEmbeddedLanguagesProvider.Info,
        VisualBasicSyntaxKinds.Instance,
        services)
{
}
