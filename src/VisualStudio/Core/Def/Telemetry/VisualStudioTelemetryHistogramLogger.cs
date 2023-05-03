// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class VisualStudioTelemetryHistogramLogger : TelemetryHistogramLogger
    {
        private readonly TelemetrySession _session;
        private readonly VSTelemetryMeterProvider _meterProvider;
        private readonly Dictionary<string, TelemetryHistogramLoggerForFeature> _histogramLoggers;
        private readonly int BatchedTelemetryCollectionPeriodInSeconds = 10;
        private readonly object _lock;

        private VisualStudioTelemetryHistogramLogger(TelemetrySession session)
        {
            _session = session;
            _meterProvider = new();
            _histogramLoggers = new();

            _ = PostCollectedTelemetryAsync();

            _lock = new object();
        }

        public static void CreateTelemetryHistogramLogger(TelemetrySession session)
        {
            var histogramLogger = new VisualStudioTelemetryHistogramLogger(session);

            TelemetryHistogramLogger.SetLogger(histogramLogger);
        }

        public override IDisposable LogBlock(string featureName, string metricName)
        {
            TelemetryHistogramLoggerForFeature? histogramLogger;

            lock (_lock)
            {
                if (!_histogramLoggers.TryGetValue(featureName, out histogramLogger))
                {
                    var meterName = "vs.ide.vbcs.perf." + featureName;
                    var eventName = "vs/ide/vbcs/perf/" + featureName;
                    var meter = _meterProvider.CreateMeter(meterName, version: "0.23");

                    histogramLogger = new TelemetryHistogramLoggerForFeature(meter, eventName);
                    _histogramLoggers.Add(featureName, histogramLogger);
                }
            }

            return histogramLogger.LogBlockTimed(metricName);
        }

        private async Task PostCollectedTelemetryAsync()
        {
            while (true)
            {
                await Task.Delay(BatchedTelemetryCollectionPeriodInSeconds * 1000).ConfigureAwait(false);

                lock (_lock)
                {
                    foreach (var histogramLogger in _histogramLoggers.Values)
                    {
                        histogramLogger.PostTelemetry(_session);
                    }

                    _histogramLoggers.Clear();
                }
            }
        }

        private class TelemetryHistogramLoggerForFeature
        {
            private readonly IMeter _meter;
            private readonly string _eventName;
            private readonly Dictionary<string, IHistogram<int>> _histograms;
            private readonly object _lock;

            public TelemetryHistogramLoggerForFeature(IMeter meter, string eventName)
            {
                _meter = meter;
                _eventName = eventName;
                _histograms = new();
                _lock = new();
                _eventName = eventName;
            }

            public void Log(string metricName, int value)
            {
                lock (_lock)
                {
                    if (!_histograms.TryGetValue(metricName, out var histogram))
                    {
                        histogram = _meter.CreateHistogram<int>(metricName);
                        _histograms.Add(metricName, histogram);
                    }

                    histogram.Record(value);
                }
            }

            public void PostTelemetry(TelemetrySession session)
            {
                lock (_lock)
                {
                    foreach (var histogram in _histograms.Values)
                    {
                        var telemetryEvent = new TelemetryEvent(_eventName);
                        var histogramEvent = new TelemetryHistogramEvent<int>(telemetryEvent, histogram);

                        session.PostMetricEvent(histogramEvent);
                    }

                    _histograms.Clear();
                }
            }

            public IDisposable LogBlockTimed(string metricName)
            {
                return new TelemetryLogBlock(metricName, this);
            }
        }

        private class TelemetryLogBlock : IDisposable
        {
            private readonly string _metricName;
            private readonly SharedStopwatch _stopwatch;
            private readonly TelemetryHistogramLoggerForFeature _histogramLogger;

            public TelemetryLogBlock(string metricName, TelemetryHistogramLoggerForFeature histogramLogger)
            {
                _metricName = metricName;
                _histogramLogger = histogramLogger;
                _stopwatch = SharedStopwatch.StartNew();
            }

            public void Dispose()
            {
                var elapsed = (int)_stopwatch.Elapsed.TotalMilliseconds;
                _histogramLogger.Log(_metricName, elapsed);
            }
        }
    }
}
