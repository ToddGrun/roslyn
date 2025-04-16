// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Progression.V2.Items;

internal class BaseAttachedCollectionSource(IVsHierarchyItem Item) : IAttachedCollectionSource
{
    public object SourceItem => Item;

    public bool HasItems => throw new NotImplementedException();

    public IEnumerable Items => throw new NotImplementedException();
}
