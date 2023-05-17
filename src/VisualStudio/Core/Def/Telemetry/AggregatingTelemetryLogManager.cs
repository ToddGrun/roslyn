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
    internal class AggregatingTelemetryLogManager : IDisposable
    {
        private readonly TelemetrySession _session;
        private readonly Dictionary<FunctionId, AggregatingTelemetryLog> _aggregatingLogs;
        private readonly object _lock;
        private const int BatchedTelemetryCollectionPeriodInSeconds = 60 * 30;

        public AggregatingTelemetryLogManager(TelemetrySession session)
        {
            _session = session;
            _aggregatingLogs = new();

            _ = PostCollectedTelemetryAsync();

            _lock = new object();
        }

        public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            AggregatingTelemetryLog? aggregatingLogger;

            lock (_lock)
            {
                if (!_aggregatingLogs.TryGetValue(functionId, out aggregatingLogger))
                {
                    aggregatingLogger = new AggregatingTelemetryLog(_session, functionId, bucketBoundaries);
                    _aggregatingLogs.Add(functionId, aggregatingLogger);
                }
            }

            return aggregatingLogger;
        }

        private async Task PostCollectedTelemetryAsync()
        {
            while (true)
            {
                await Task.Delay(BatchedTelemetryCollectionPeriodInSeconds * 1000).ConfigureAwait(false);

                PostCollectedTelemetry();
            }
        }

        public void Dispose()
        {
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
