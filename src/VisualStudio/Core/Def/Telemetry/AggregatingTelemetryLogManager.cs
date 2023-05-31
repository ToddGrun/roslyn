// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
    /// send aggregated events every 30 minutes.
    /// </summary>
    internal sealed class AggregatingTelemetryLogManager
    {
        private static readonly TimeSpan s_batchedTelemetryCollectionPeriod = TimeSpan.FromMinutes(30);

        private readonly TelemetrySession _session;
        private readonly AsyncBatchingWorkQueue _postTelemetryQueue;

        /// <summary>
        /// Indicates whether <see cref="_postTelemetryQueue"/> has work already added to it.
        /// </summary>
        private int _workAdded = 0;

        private ImmutableDictionary<FunctionId, AggregatingTelemetryLog> _aggregatingLogs = ImmutableDictionary<FunctionId, AggregatingTelemetryLog>.Empty;

        public AggregatingTelemetryLogManager(TelemetrySession session, IAsynchronousOperationListener asyncListener)
        {
            _session = session;

            _postTelemetryQueue = new AsyncBatchingWorkQueue(
                s_batchedTelemetryCollectionPeriod,
                PostCollectedTelemetryAsync,
                asyncListener,
                CancellationToken.None);
        }

        public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            if (!_session.IsOptedIn)
                return null;

            if (Interlocked.Exchange(ref _workAdded, 1) == 0)
            {
                // The queue doesn't have any work pending. Add an item so PostCollectedTelemetryAsync
                //   will get fired after the collection period.
                _postTelemetryQueue.AddWork();
            }

            return ImmutableInterlocked.GetOrAdd(ref _aggregatingLogs, functionId, functionId => new AggregatingTelemetryLog(_session, functionId, bucketBoundaries));
        }

        private ValueTask PostCollectedTelemetryAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            PostCollectedTelemetry();

            // Reset to indicate the queue doesn't have any work pending.
            _workAdded = 0;

            return ValueTaskFactory.CompletedTask;
        }

        private void PostCollectedTelemetry()
        {
            if (!_session.IsOptedIn)
                return;

            foreach (var log in _aggregatingLogs.Values)
            {
                log.PostTelemetry(_session);
            }
        }
    }
}
