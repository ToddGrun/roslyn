// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BracePairs;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;

namespace Microsoft.CodeAnalysis.VisualBasic.BracePairs;

[ExportLanguageService(typeof(IBracePairsService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualBasicBracePairsService()
    : AbstractBracePairsService(VisualBasicSyntaxKinds.Instance)
{
}
