// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;

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
        private bool _isDisposed;
        private readonly object _lock;
        private const int BatchedTelemetryCollectionPeriodInSeconds = 60 * 30;

        public AggregatingTelemetryLogManager(TelemetrySession session)
        {
            _session = session;
            _aggregatingLogs = new();
            _isDisposed = false;
            _lock = new object();

            _ = PostCollectedTelemetryAsync();
        }

        public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            AggregatingTelemetryLog? aggregatingLog;

            lock (_lock)
            {
                if (!_aggregatingLogs.TryGetValue(functionId, out aggregatingLog))
                {
                    aggregatingLog = new AggregatingTelemetryLog(_session, functionId, bucketBoundaries);
                    _aggregatingLogs.Add(functionId, aggregatingLog);
                }
            }

            return aggregatingLog;
        }

        private async Task PostCollectedTelemetryAsync()
        {
            while (!_isDisposed)
            {
                await Task.Delay(BatchedTelemetryCollectionPeriodInSeconds * 1000).ConfigureAwait(false);

                PostCollectedTelemetry();
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            PostCollectedTelemetry();
        }

        private void PostCollectedTelemetry()
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
