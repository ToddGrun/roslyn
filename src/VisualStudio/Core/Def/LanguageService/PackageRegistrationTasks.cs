﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

using WorkTask = Func<IProgress<ServiceProgressData>, PackageRegistrationTasks, CancellationToken, Task>;

/// <summary>
/// Provides a mechanism for registering work to be done during package initialization. Work is registered
/// as either main thread or background thread appropriate. This allows processing of these work items
/// in a batched manner, reducing the number of thread switches required during the performance sensitive
/// package loading timeframe.
/// 
/// Note that currently the processing of these tasks isn't done concurrently. A future optimization may
/// allow parallel background thread task execution, or even concurrent main and background thread work.
/// </summary>
internal sealed class PackageRegistrationTasks(JoinableTaskFactory jtf)
{
    private readonly List<WorkTask> _backgroundThreadWorkTasks = [];
    private readonly List<WorkTask> _mainThreadWorkTasks = [];
    private readonly JoinableTaskFactory _jtf = jtf;
    private readonly object _gate = new();

    public void AddTask(bool isMainThreadTask, WorkTask task)
    {
        // This lock is a bit extraneous, as the current code doesn't register or process tasks concurrently.
        lock (_gate)
        {
            var workTasks = GetWorkTasks(isMainThreadTask);
            workTasks.Add(task);
        }
    }

    public async Task ProcessTasksAsync(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
    {
        using var _1 = DebugInfo.AddScopedInfo("PackageRegistrationTasks.ProcessTasksAsync - 0");

        // prime the pump by doing the first group of bg thread work if the initiating thread is not the main thread
        if (!_jtf.Context.IsOnMainThread)
        {
            await PerformWorkAsync(isMainThreadTask: false, progress, cancellationToken).ConfigureAwait(false);
            DebugInfo.AddInfo("PackageRegistrationTasks.ProcessTasksAsync - prime");
        }

        // Continue processing work until everything is completed, switching between main and bg threads as needed.
        while (_mainThreadWorkTasks.Count > 0 || _backgroundThreadWorkTasks.Count > 0)
        {
            await PerformWorkAsync(isMainThreadTask: true, progress, cancellationToken).ConfigureAwait(false);
            DebugInfo.AddInfo("PackageRegistrationTasks.ProcessTasksAsync - loop(main)");

            await PerformWorkAsync(isMainThreadTask: false, progress, cancellationToken).ConfigureAwait(false);
            DebugInfo.AddInfo("PackageRegistrationTasks.ProcessTasksAsync - loop(bg)");
        }
    }

    private List<WorkTask> GetWorkTasks(bool isMainThreadTask)
        => isMainThreadTask ? _mainThreadWorkTasks : _backgroundThreadWorkTasks;

    private async Task PerformWorkAsync(bool isMainThreadTask, IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
    {
        List<WorkTask> workTasks;

        // This lock is a bit extraneous, as the current code doesn't register or process tasks concurrently.
        lock (_gate)
        {
            var currentWorkTasks = GetWorkTasks(isMainThreadTask);
            workTasks = [.. currentWorkTasks];
            currentWorkTasks.Clear();
        }

        if (workTasks.Count == 0)
            return;

        // Ensure we're invoking the task on the right thread
        if (isMainThreadTask)
            await _jtf.SwitchToMainThreadAsync(cancellationToken);
        else if (_jtf.Context.IsOnMainThread)
            await TaskScheduler.Default;

        for (var i = 0; i < workTasks.Count; i++)
        {
            var work = workTasks[i];

            // CA(true) is important here, as we want to ensure that each iteration is done in the same
            // captured context. Thus, even poorly behaving tasks (ie, those that do their own thread switching)
            // don't effect the next loop iteration.
            await work(progress, this, cancellationToken).ConfigureAwait(true);
        }
    }
}
