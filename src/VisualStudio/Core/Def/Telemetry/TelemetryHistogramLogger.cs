// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class TelemetryHistogramLogger : ITelemetryHistogramLogger
    {
        private readonly IMeter _meter;
        private readonly TelemetrySession _session;
        private readonly Dictionary<string, IHistogram<int>> _histograms;
        private readonly object _lock;
        private readonly HistogramConfiguration? _histogramConfiguration;
        private readonly string _eventName;

        private const string MeterVersion = "0.35";

        public TelemetryHistogramLogger(TelemetrySession session, FunctionId functionId, double[]? bucketBoundaries = null)
        {
            var meterName = TelemetryLogger.GetPropertyName(functionId, "meter");
            var meterProvider = new VSTelemetryMeterProvider();

            _session = session;
            _meter = meterProvider.CreateMeter(meterName, version: MeterVersion);
            _histograms = new();
            _lock = new();
            _eventName = TelemetryLogger.GetEventName(functionId);

            if (bucketBoundaries != null)
            {
                _histogramConfiguration = new HistogramConfiguration(bucketBoundaries);
            }
        }

        public void Log(string metricName, int value)
        {
            if (!IsEnabled)
                return;

            lock (_lock)
            {
                if (!_histograms.TryGetValue(metricName, out var histogram))
                {
                    histogram = _meter.CreateHistogram<int>(metricName, _histogramConfiguration);
                    _histograms.Add(metricName, histogram);
                }

                histogram.Record(value);
            }
        }

        public IDisposable? LogBlockTimed(string metricName)
        {
            if (!IsEnabled)
                return null;

            return new TimedTelemetryHistogramLogBlock(metricName, this);
        }

        private bool IsEnabled
        {
            get
            {
#if DEBUG
                return false;
#else
                return _session.IsOptedIn && !Debugger.IsAttached;
#endif
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
    }
}
