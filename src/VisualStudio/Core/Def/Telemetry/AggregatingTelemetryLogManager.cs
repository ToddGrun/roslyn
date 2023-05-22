// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Manages creation and obtaining aggregated telemetry logs. Also, notifies logs to
    /// send aggregated events every 30 minutes or upon disposal.
    /// </summary>
    internal sealed class AggregatingTelemetryLogManager : IDisposable
    {
        private readonly TelemetrySession _session;
        private readonly Dictionary<FunctionId, AggregatingTelemetryLog> _aggregatingLogs;
        private readonly object _lock;
        private const int BatchedTelemetryCollectionPeriodInSeconds = 60 * 30;
        private readonly AsyncBatchingWorkQueue _postTelemetryQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public AggregatingTelemetryLogManager(TelemetrySession session, IAsynchronousOperationListener asyncListener)
        {
            _session = session;
            _aggregatingLogs = new();
            _lock = new object();
            _cancellationTokenSource = new();

            _postTelemetryQueue = new AsyncBatchingWorkQueue(
                TimeSpan.FromSeconds(BatchedTelemetryCollectionPeriodInSeconds),
                PostCollectedTelemetryAsync,
                asyncListener,
                _cancellationTokenSource.Token);

            _postTelemetryQueue.AddWork();
        }

        public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            AggregatingTelemetryLog? aggregatingLog = null;

            if (_session.IsOptedIn)
            {
                lock (_lock)
                {
                    if (!_aggregatingLogs.TryGetValue(functionId, out aggregatingLog))
                    {
                        aggregatingLog = new AggregatingTelemetryLog(_session, functionId, bucketBoundaries);
                        _aggregatingLogs.Add(functionId, aggregatingLog);
                    }
                }
            }

            return aggregatingLog;
        }

        public void Dispose()
        {
            // Cancel any pending work, instead posting telemetry immediately
            _cancellationTokenSource.Dispose();

            PostCollectedTelemetry();
        }

        private ValueTask PostCollectedTelemetryAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            PostCollectedTelemetry();

            // Add another work item to the queue as we want this continue executing until disposal.
            // The AddWork call below is a no-op if we've been Disposed since checking
            // the cancellation token.
            _postTelemetryQueue.AddWork();

            return ValueTaskFactory.CompletedTask;
        }

        private void PostCollectedTelemetry()
        {
            if (_session.IsOptedIn)
            {
                lock (_lock)
                {
                    foreach (var log in _aggregatingLogs.Values)
                    {
                        log.PostTelemetry(_session);
                    }
                }
            }
        }
    }
}
