// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportLanguageServiceFactory(typeof(ITypeImportCompletionService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TypeImportCompletionServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        return new BasicTypeImportCompletionService(languageServices.LanguageServices.SolutionServices);
    }

    private class BasicTypeImportCompletionService(SolutionServices services) : AbstractTypeImportCompletionService(services)
    {
        protected override string GenericTypeSuffix => "(Of ...)";

        protected override bool IsCaseSensitive => false;

        protected override string Language => LanguageNames.VisualBasic;
    }
}
