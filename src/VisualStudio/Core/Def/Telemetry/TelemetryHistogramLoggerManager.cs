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
    internal class TelemetryHistogramLoggerManager : ITelemetryHistogramLoggerProvider, IDisposable
    {
        private readonly TelemetrySession _session;
        private readonly Dictionary<FunctionId, TelemetryHistogramLogger> _histogramLoggers;
        private readonly object _lock;
        private const int BatchedTelemetryCollectionPeriodInSeconds = 60 * 30;

        private TelemetryHistogramLoggerManager(TelemetrySession session)
        {
            _session = session;
            _histogramLoggers = new();

            _ = PostCollectedTelemetryAsync();

            _lock = new object();
        }

        public static TelemetryHistogramLoggerManager Create(TelemetrySession session)
        {
            var logger = new TelemetryHistogramLoggerManager(session);

            TelemetryHistogram.SetLoggerProvider(logger);

            return logger;
        }

        public ITelemetryHistogramLogger? GetLogger(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            TelemetryHistogramLogger? histogramLogger;

            lock (_lock)
            {
                if (!_histogramLoggers.TryGetValue(functionId, out histogramLogger))
                {
                    histogramLogger = new TelemetryHistogramLogger(_session, functionId, bucketBoundaries);
                    _histogramLoggers.Add(functionId, histogramLogger);
                }
            }

            return histogramLogger;
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
                foreach (var histogramLogger in _histogramLoggers.Values)
                {
                    histogramLogger.PostTelemetry(_session);
                }
            }
        }
    }
}
