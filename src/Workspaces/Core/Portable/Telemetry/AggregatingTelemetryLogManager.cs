// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Manages creation and obtaining aggregated telemetry logs. Also, notifies logs to
    /// send aggregated events every 30 minutes.
    /// </summary>
    internal sealed class AggregatingTelemetryLogManager
    {
        private static readonly TimeSpan s_batchedTelemetryCollectionPeriod = TimeSpan.FromMinutes(30);

        private readonly ITelemetryLogProvider _telemetryLogProvider;
        private readonly AsyncBatchingWorkQueue _postTelemetryQueue;

        private ImmutableDictionary<FunctionId, ITelemetryLog> _aggregatingLogs = ImmutableDictionary<FunctionId, ITelemetryLog>.Empty;

        public AggregatingTelemetryLogManager(ITelemetryLogProvider telemetryLogProvider, IAsynchronousOperationListener asyncListener)
        {
            _telemetryLogProvider = telemetryLogProvider;

            _postTelemetryQueue = new AsyncBatchingWorkQueue(
                s_batchedTelemetryCollectionPeriod,
                PostCollectedTelemetryAsync,
                asyncListener,
                CancellationToken.None);
        }

        public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            if (!_telemetryLogProvider.IsOptedIn)
                return null;

            return ImmutableInterlocked.GetOrAdd(ref _aggregatingLogs, functionId, functionId => _telemetryLogProvider.CreateAggregatingLog(functionId, bucketBoundaries));
        }

        public void EnsureTelemetryWorkQueued()
        {
            // Ensure PostCollectedTelemetryAsync will get fired after the collection period.
            _postTelemetryQueue.AddWork();
        }

        private ValueTask PostCollectedTelemetryAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            PostCollectedTelemetry();

            return ValueTaskFactory.CompletedTask;
        }

        private void PostCollectedTelemetry()
        {
            if (!_telemetryLogProvider.IsOptedIn)
                return;

            foreach (var log in _aggregatingLogs.Values)
            {
                _telemetryLogProvider.PostTelemetry(log);
            }
        }
    }
}
