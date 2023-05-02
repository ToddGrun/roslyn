// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private readonly Timer _timer;
        private readonly object _lock;

        private VisualStudioTelemetryHistogramLogger(TelemetrySession session)
        {
            _session = session;
            _meterProvider = new();
            _histogramLoggers = new();

            _timer = new Timer(OnTimerCallback, state: null, dueTime: 0, period: 30 * 1000);
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
                    var meter = _meterProvider.CreateMeter("vs/ide/vbcs/perf/" + featureName, version: "0.1");

                    histogramLogger = new TelemetryHistogramLoggerForFeature(meter);
                    _histogramLoggers.Add(featureName, histogramLogger);
                }
            }

            return histogramLogger.LogBlockTimed(metricName);
        }

        private void OnTimerCallback(object? state)
        {
            lock (_lock)
            {
                foreach (var histogramLogger in _histogramLoggers.Values)
                {
                    histogramLogger.PostTelemetry(_session);
                }

                _histogramLoggers.Clear();
            }
        }

        private class TelemetryHistogramLoggerForFeature
        {
            private readonly IMeter _meter;
            private readonly Dictionary<string, IHistogram<int>> _histograms;
            private readonly object _lock;

            public TelemetryHistogramLoggerForFeature(IMeter meter)
            {
                _meter = meter;
                _histograms = new();
                _lock = new();
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

                    if (histogram is not IVSHistogram<int> vsHistogram)
                    {
                        return;
                    }

                    var oldCount = vsHistogram.Statistics.Counter.Count;
                    var oldBuckets = vsHistogram.Buckets;
                    var oldBucketsEnum = (System.Collections.IEnumerable)oldBuckets.GetType().InvokeMember("Buckets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty, null, oldBuckets, null);
                    long oldCountFromBuckets = 0;
                    foreach(var oldBucket in oldBucketsEnum)
                    {
                        var oldStat = (HistogramStatistics<int>) oldBucket.GetType().InvokeMember("Statistics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty, null, oldBucket, null);
                        oldCountFromBuckets += oldStat.Counter.Count;
                    }

                    histogram.Record(value);

                    var newCount = vsHistogram.Statistics.Counter.Count;
                    var newBuckets = vsHistogram.Buckets;
                    var newBucketsEnum = (System.Collections.IEnumerable)newBuckets.GetType().InvokeMember("Buckets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty, null, newBuckets, null);
                    long newCountFromBuckets = 0;
                    foreach (var newBucket in newBucketsEnum)
                    {
                        var newStat = (HistogramStatistics<int>)newBucket.GetType().InvokeMember("Statistics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty, null, newBucket, null);
                        newCountFromBuckets += newStat.Counter.Count;
                    }

                    if (oldCount + 1 != newCount || oldCountFromBuckets + 1 != newCountFromBuckets)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
            }

            public void PostTelemetry(TelemetrySession session)
            {
                lock (_lock)
                {
                    foreach (var histogram in _histograms.Values)
                    {
                        var telemetryEvent = new TelemetryEvent(_meter.Name);
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
