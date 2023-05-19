// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Provides a wrapper around the VSTelemetry histogram APIs to support aggregated telemetry. Each instance
    /// of this class corresponds to a specific FunctionId operation and can support aggregated values for each
    /// metric name logged.
    /// </summary>
    internal sealed class AggregatingTelemetryLog : ITelemetryLog
    {
        private readonly IMeter _meter;
        private readonly TelemetrySession _session;
        private readonly Dictionary<string, IHistogram<int>> _histograms;
        private readonly object _lock;
        private readonly HistogramConfiguration? _histogramConfiguration;
        private readonly string _eventName;

        private const string MeterVersion = "0.39";

        public AggregatingTelemetryLog(TelemetrySession session, FunctionId functionId, double[]? bucketBoundaries)
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

        /// <summary>
        /// Adds aggregated information for the metric and value passed in via logMessage. KeyValueLogMessage.Properties
        /// is searched for appropriate entries to determine the metric name and value.
        /// </summary>
        /// <param name="logMessage"></param>
        public void Log(LogMessage logMessage)
        {
            if (!IsEnabled)
                return;

            if (logMessage is not KeyValueLogMessage kvLogMessage)
                return;

            string? metricName = null;
            int? value = null;
            foreach (var kvp in kvLogMessage.Properties)
            {
                switch (kvp.Value)
                {
                    case string s:
                        metricName = s;
                        break;
                    case int i:
                        value = i;
                        break;
                }
            }

            if (metricName is null || value is null)
                return;

            IHistogram<int>? histogram;
            lock (_lock)
            {
                if (!_histograms.TryGetValue(metricName, out histogram))
                {
                    histogram = _meter.CreateHistogram<int>(metricName, _histogramConfiguration);
                    _histograms.Add(metricName, histogram);
                }
            }

            histogram.Record(value.Value);
        }

        public IDisposable? LogBlockTime(string name, int minThreshold)
        {
            if (!IsEnabled)
                return null;

            return new TimedTelemetryLogBlock(name, minThreshold, this);
        }

        private bool IsEnabled
        {
            get
            {
                return _session.IsOptedIn;
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
