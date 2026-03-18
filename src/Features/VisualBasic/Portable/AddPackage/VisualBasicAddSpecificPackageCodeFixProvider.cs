// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.AddPackage;

[ExportCodeFixProvider(LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.AddPackage), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicAddSpecificPackageCodeFixProvider()
    : AbstractAddSpecificPackageCodeFixProvider
{
    private const string BC37267 = nameof(BC37267); // Predefined type 'ValueTuple(Of ,)' is not defined or imported.

    public override ImmutableArray<string> FixableDiagnosticIds => [BC37267];

    protected override string GetAssemblyName(string id)
    {
        return id switch
        {
            BC37267 => "System.ValueTuple",
            _ => null
        };
    }
}
