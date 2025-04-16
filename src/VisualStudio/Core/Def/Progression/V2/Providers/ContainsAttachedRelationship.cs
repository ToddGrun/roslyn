// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Progression.V2.Providers;

internal sealed class ContainsAttachedRelationship : IAttachedRelationship
{
    public string Name
    {
        get { return KnownRelationships.Contains; }
    }

    public string DisplayName
    {
        get { return ServicesVSResources.Relationship_ContainsText; }
    }
}
