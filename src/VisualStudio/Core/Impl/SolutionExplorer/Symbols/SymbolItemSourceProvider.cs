// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(SymbolItemSourceProvider)), Order]
[AppliesToProject("CSharp | VB")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SymbolItemSourceProvider(
    IThreadingContext threadingContext,
    VisualStudioWorkspace workspace)
    : IAttachedCollectionSourceProvider
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly Workspace _workspace = workspace;

    private IHierarchyItemToProjectIdMap? _projectMap;

    public IAttachedCollectionSource? CreateCollectionSource(object item, string relationshipName)
    {
        return item switch
        {
            IVsHierarchyItem hierarchyItem => CreateCollectionSource(hierarchyItem, relationshipName),
            SymbolItem symbolItem => CreateCollectionSource(symbolItem, relationshipName),
            _ => null
        };
    }

    public IEnumerable<IAttachedRelationship> GetRelationships(object item)
    {
        return [];
    }

    private IAttachedCollectionSource? CreateCollectionSource(SymbolItem item, string relationshipName)
    {
        if (relationshipName == KnownRelationships.Contains)
        {
            return new SymbolItemSource(item, _threadingContext, _workspace, item.DocumentId);
        }

        return null;
    }

    private IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
    {
        if (item.HierarchyIdentity?.NestedHierarchy != null &&
            relationshipName == KnownRelationships.Contains)
        {
            var hierarchyMapper = TryGetProjectMap();
            if (hierarchyMapper != null &&
                hierarchyMapper.TryGetProjectId(item, targetFrameworkMoniker: null, out var projectId))
            {
                if (item.HierarchyIdentity.NestedHierarchy.GetCanonicalName(item.HierarchyIdentity.NestedItemID, out var canonicalName2) == VSConstants.S_OK)
                {
                    var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(canonicalName2).FirstOrDefault(static (d, projectId) => d.ProjectId == projectId, projectId);

                    if (documentId != null)
                        return new SymbolItemSource(item, _threadingContext, _workspace, documentId);
                }
            }
        }

        return null;
    }

    private IHierarchyItemToProjectIdMap? TryGetProjectMap()
        => _projectMap ??= _workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
}
