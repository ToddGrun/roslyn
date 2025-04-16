// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Progression.V2.Providers;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(RoslynGraphProviderNew))]
[Order(After = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RoslynGraphProviderNew()
    : AttachedCollectionSourceProvider<IVsHierarchyItem>
{
    protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
    {
        return null;
    }
}
