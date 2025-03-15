// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    private readonly List<WorkTask> _backgroundThreadTasks = [];
    private readonly List<WorkTask> _mainThreadTasks = [];
    private readonly JoinableTaskFactory _jtf = jtf;
    private readonly object _gate = new();

    public void AddTask(bool isMainThreadTask, WorkTask task)
    {
        var workTasks = GetTasks(isMainThreadTask);

        lock (_gate)
        {
            workTasks.Add(task);
        }
    }

    public static List<string> s_debugInfo = new();

    public async Task ProcessTasksAsync(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
    {
        var sw1 = Stopwatch.StartNew();
        Task[] backgroundThreadTasks;
        Task mainThreadTask;

        var loopIter = 0;
        while (_mainThreadTasks.Count > 0 || _backgroundThreadTasks.Count > 0)
        {
            var oldMainThreadCount = _mainThreadTasks.Count;
            var oldBackgroundThreadCount = _backgroundThreadTasks.Count;

            var sw2 = Stopwatch.StartNew();
            // This lock usage is extraneous as AddTask can only be called during the awaiting of
            // mainThreadTask and backgroundThreadTask after the lock is released.
            lock (_gate)
            {
                GetBatchedBackgroundTasks_UnderLock(progress, cancellationToken, out backgroundThreadTasks);
                GetBatchedMainThreadTask_UnderLock(progress, cancellationToken, out mainThreadTask);
            }

            var backgroundThreadTask = Task.WhenAll(backgroundThreadTasks);
            await mainThreadTask.ConfigureAwait(false);
            s_debugInfo.Add($"Loop {loopIter} main, cnt={oldMainThreadCount}, time={sw2.ElapsedMilliseconds}");

            await backgroundThreadTask.ConfigureAwait(false);

            s_debugInfo.Add($"Loop {loopIter++}, cnt={oldBackgroundThreadCount}, time= {sw2.ElapsedMilliseconds}");
        }

        s_debugInfo.Add($"Total: {sw1.ElapsedMilliseconds}");
    }

    private void GetBatchedBackgroundTasks_UnderLock(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken, out Task[] backgroundThreadTasks)
    {
        backgroundThreadTasks = _backgroundThreadTasks.Select(workTask => Task.Run(async () =>
        {
            if (_jtf.Context.IsOnMainThread)
                await TaskScheduler.Default;
            else
                await Task.Yield();

            await workTask(progress, this, cancellationToken).ConfigureAwait(false);
        })).ToArray();

        _backgroundThreadTasks.Clear();
    }

    private void GetBatchedMainThreadTask_UnderLock(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken, out Task mainThreadTask)
    {
        var mainThreadTasks = _mainThreadTasks.ToArray();

        mainThreadTask = _mainThreadTasks.Count == 0
            ? Task.CompletedTask
            : Task.Run(
                async () =>
                {
                    await _jtf.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
                    foreach (var workTask in mainThreadTasks)
                    {
                        // CA(true) is important here, as we want to ensure that each iteration is done on the main thead.
                        // Thus, even poorly behaving tasks (ie, those that do their own thread switching) don't effect
                        // the next loop iteration.
                        await workTask(progress, this, cancellationToken).ConfigureAwait(true);
                    }
                }, CancellationToken.None);

        _mainThreadTasks.Clear();
    }

    private List<WorkTask> GetTasks(bool isMainThreadTask)
        => isMainThreadTask ? _mainThreadTasks : _backgroundThreadTasks;
}
