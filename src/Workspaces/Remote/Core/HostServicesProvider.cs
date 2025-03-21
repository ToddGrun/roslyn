// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Host.Mef;

[Export(typeof(HostServicesProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class HostServicesProvider(ExportProvider exportProvider)
{
    public HostServices HostServices { get; } = VisualStudioMefHostServices.Create(exportProvider);
}
