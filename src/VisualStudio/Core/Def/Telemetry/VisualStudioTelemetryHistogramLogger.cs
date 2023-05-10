// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class VisualStudioTelemetryHistogramLogger : ITelemetryHistogramLogger, IDisposable
    {
        private readonly TelemetrySession _session;
        private readonly VSTelemetryMeterProvider _meterProvider;
        private readonly Dictionary<string, TelemetryHistogramLoggerForFeature> _histogramLoggers;
        private readonly object _lock;
        private const int BatchedTelemetryCollectionPeriodInSeconds = 30;
        private const string MeterVersion = "0.29";

        private VisualStudioTelemetryHistogramLogger(TelemetrySession session)
        {
            _session = session;
            _meterProvider = new();
            _histogramLoggers = new();

            _ = PostCollectedTelemetryAsync();

            _lock = new object();
        }

        public static VisualStudioTelemetryHistogramLogger CreateTelemetryHistogramLogger(TelemetrySession session)
        {
            var histogramLogger = new VisualStudioTelemetryHistogramLogger(session);

            TelemetryHistogramLogger.SetLogger(histogramLogger);

            return histogramLogger;
        }

        public IDisposable? LogBlockTimed(string featureName, string metricName)
        {
            if (!IsEnabled())
                return null;

            var histogramLogger = GetHistogramLogger(featureName);

            return histogramLogger.LogBlockTimed(featureName, metricName);
        }

        public void Log(string featureName, string metricName, int value)
        {
            if (!IsEnabled())
                return;

            var histogramLogger = GetHistogramLogger(featureName);

            histogramLogger.Log(featureName, metricName, value);
        }

        private bool IsEnabled()
        {
            if (!_session.IsOptedIn)
                return false;

            if (Debugger.IsAttached)
                return false;

#if DEBUG
            return false;
#else
            return true;
#endif
        }

        private TelemetryHistogramLoggerForFeature GetHistogramLogger(string featureName)
        {
            TelemetryHistogramLoggerForFeature? histogramLogger;

            lock (_lock)
            {
                if (!_histogramLoggers.TryGetValue(featureName, out histogramLogger))
                {
                    var meterName = "vs.ide.vbcs.perf." + featureName;
                    var meter = _meterProvider.CreateMeter(meterName, version: MeterVersion);

                    histogramLogger = new TelemetryHistogramLoggerForFeature(meter, featureName);
                    _histogramLoggers.Add(featureName, histogramLogger);
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
