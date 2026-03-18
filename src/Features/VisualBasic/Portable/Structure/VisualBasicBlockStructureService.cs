// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

[ExportLanguageServiceFactory(typeof(BlockStructureService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicBlockStructureServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        return new VisualBasicBlockStructureService(languageServices.LanguageServices.SolutionServices);
    }
}

internal class VisualBasicBlockStructureService : BlockStructureServiceWithProviders
{
    internal VisualBasicBlockStructureService(SolutionServices services)
        : base(services)
    {
    }

    public override string Language => LanguageNames.VisualBasic;

    protected override ImmutableArray<BlockStructureProvider> GetBuiltInProviders()
    {
        return [new VisualBasicBlockStructureProvider()];
    }
}
