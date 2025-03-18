// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract class AbstractPackage : AsyncPackage
{
    private IComponentModel? _componentModel_doNotAccessDirectly;

    internal IComponentModel ComponentModel
    {
        get
        {
            Assumes.Present(_componentModel_doNotAccessDirectly);
            return _componentModel_doNotAccessDirectly;
        }
    }

    protected virtual void RegisterInitializeAsyncWork(PackageLoadTasks packageInitializeTasks)
    {
        // This treatment of registering work on the bg/main threads is a bit unique as we want the component model initialized at the beginning
        // of whichever context is invoked first. The current architecture doesn't execute any of the registered tasks concurrently,
        // so that isn't a concern for running calculating or setting _componentModel_doNotAccessDirectly multiple times.
        packageInitializeTasks.AddTask(isMainThreadTask: false, task: EnsureComponentModelAsync);
        packageInitializeTasks.AddTask(isMainThreadTask: true, task: EnsureComponentModelAsync);

        async Task EnsureComponentModelAsync(PackageLoadTasks packageInitializeTasks, CancellationToken token)
        {
            if (_componentModel_doNotAccessDirectly == null)
            {
                _componentModel_doNotAccessDirectly = (IComponentModel?)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(false);
                Assumes.Present(_componentModel_doNotAccessDirectly);
            }
        }
    }

    /// This method is called upon package creation and is the mechanism by which roslyn packages calculate and
    /// process all package initialization work. Do not override this sealed method, instead override RegisterInitializationWork
    /// to indicate the work your package needs upon initialization.
    protected sealed override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        var packageInitializeTasks = new PackageLoadTasks(JoinableTaskFactory);

        // Request all initially known work, classified into whether it should be processed on the main or
        // background thread. These task collections can be modified by the work itself to add more work for subsequent processing.
        // Requesting this information is useful as it lets us batch up work on these threads, significantly
        // reducing thread switches during package load.
        RegisterInitializeAsyncWork(packageInitializeTasks);

        await packageInitializeTasks.ProcessTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    protected virtual void RegisterOnAfterPackageLoadedAsyncWork(PackageLoadTasks packageLoadedTasks)
    {
        packageLoadedTasks.AddTask(
            isMainThreadTask: false,
            task: (packageLoadedTasks, cancellationToken) =>
            {
                // TODO: remove, workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1985204
                var globalOptions = ComponentModel.GetService<IGlobalOptionService>();
                if (globalOptions.GetOption(SemanticSearchFeatureFlag.Enabled))
                {
                    packageLoadedTasks.AddTask(
                        isMainThreadTask: true,
                        task: (packageLoadedTasks, cancellationToken) =>
                        {
                            UIContext.FromUIContextGuid(new Guid(SemanticSearchFeatureFlag.UIContextId)).IsActive = true;

                            return Task.CompletedTask;
                        });
                }

                return Task.CompletedTask;
            });
    }

    protected sealed override async Task OnAfterPackageLoadedAsync(CancellationToken cancellationToken)
    {
        var packageLoadedTasks = new PackageLoadTasks(JoinableTaskFactory);

        // Request all initially known work, classified into whether it should be processed on the main or
        // background thread. These task collections can be modified by the work itself to add more work for subsequent processing.
        // Requesting this information is useful as it lets us batch up work on these threads, significantly
        // reducing thread switches during package load.
        RegisterOnAfterPackageLoadedAsyncWork(packageLoadedTasks);

        await packageLoadedTasks.ProcessTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in other scenarios.
        // Trying to await them will throw.
        if (!KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsZombie)
        {
            await KnownUIContexts.SolutionExistsAndFullyLoadedContext;
            await LoadComponentsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract Task LoadComponentsAsync(CancellationToken cancellationToken);
}
