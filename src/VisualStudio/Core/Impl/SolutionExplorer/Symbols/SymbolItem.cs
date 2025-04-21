// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SymbolItem(
    IThreadingContext threadingContext,
    Workspace workspace,
    DocumentId documentId,
    ISymbol symbol)
    : BaseItem(name: symbol.ToDisplayString())
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly Workspace _workspace = workspace;
    public ISymbol Symbol = symbol;

    public DocumentId DocumentId { get; } = documentId;

    public override IInvocationController InvocationController
        => new InvocationControllerImpl(_threadingContext);

    private sealed class InvocationControllerImpl(IThreadingContext threadingContext) : IInvocationController
    {
        private readonly IThreadingContext _threadingContext = threadingContext;

        public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
        {
            return _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var didNavigate = false;
                foreach (var item in items.OfType<SymbolItem>())
                {
                    var documentNavigationService = item._workspace.Services.GetService<IDocumentNavigationService>();
                    if (documentNavigationService != null)
                    {
                        // TODO: we're navigating back to the top of the file, do we have a way to just bring it to the focus and that's it?
                        // TODO: Use a threaded-wait-dialog here so we can cancel navigation.
                        didNavigate |= await documentNavigationService.TryNavigateToPositionAsync(
                            item._threadingContext, item._workspace, item.DocumentId, position: 0, CancellationToken.None).ConfigureAwait(false);
                    }
                }

                return didNavigate;
            });
        }
    }
}
