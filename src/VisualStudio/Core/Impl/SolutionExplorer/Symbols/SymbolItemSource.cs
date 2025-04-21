// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal class SymbolItemSource : IAttachedCollectionSource
{
    private readonly ObservableCollection<SymbolItem> _containedItems = [];
    private ImmutableArray<ISymbol> _containedSymbols;
    private bool _isCollectionChanged = true;
    private readonly IThreadingContext _threadingContext;
    private readonly Workspace _workspace;
    private readonly DocumentId _documentId;

    public SymbolItemSource(IVsHierarchyItem item, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId)
    {
        SourceItem = item;
        _threadingContext = threadingContext;
        _workspace = workspace;
        _documentId = documentId;

        _ = Task.Run(async () =>
        {
            var document = _workspace.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                _containedSymbols = await SymbolContainment.GetContainedSymbolsAsync(document, CancellationToken.None).ConfigureAwait(false);
                _isCollectionChanged = true;
                _containedItems.Clear();
            }
        });
    }

    public SymbolItemSource(SymbolItem symbolItem, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId)
    {
        SourceItem = symbolItem;
        _threadingContext = threadingContext;
        _workspace = workspace;
        _documentId = documentId;

        _ = Task.Run(() =>
        {
            _containedSymbols = SymbolContainment.GetContainedSymbols(symbolItem.Symbol);
            _isCollectionChanged = true;
            _containedItems.Clear();
        });
    }

    public object SourceItem { get; }

    public bool HasItems => _isCollectionChanged || _containedItems.Count > 0;

    public IEnumerable Items
    {
        get
        {
            if (_isCollectionChanged)
            {
                _isCollectionChanged = false;
                foreach (var symbol in _containedSymbols)
                    _containedItems.Add(new SymbolItem(_threadingContext, _workspace, _documentId, symbol));
            }

            return _containedItems;
        }
    }
}
