// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class TelemetryHistogramLoggerForFeature : ITelemetryHistogramLogger
    {
        private struct HistogramBucket
        {
            public int Count { get; set; }
            public int Sum { get; set; }
        }

        private readonly IMeter _meter;
        private readonly string _featureName;
        private readonly Dictionary<string, IHistogram<int>> _histograms;
        private readonly object _lock;

        private static readonly double[] s_defaultHistogramBuckets = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000 };

        private readonly double[] _bucketBoundaries;
        private readonly Dictionary<string, HistogramBucket[]> _featureBuckets;

        public TelemetryHistogramLoggerForFeature(IMeter meter, string featureName, double[]? bucketBoundaries = null)
        {
            _meter = meter;
            _featureName = featureName;
            _bucketBoundaries = bucketBoundaries ?? s_defaultHistogramBuckets;
            _histograms = new();
            _lock = new();

            _featureBuckets = new();
        }

        public void Log(string featureName, string metricName, int value)
        {
            lock (_lock)
            {
                if (!_histograms.TryGetValue(metricName, out var histogram))
                {
                    histogram = _meter.CreateHistogram<int>(metricName);
                    _histograms.Add(metricName, histogram);
                }

                histogram.Record(value);

                if (!_featureBuckets.TryGetValue(metricName, out var histogramBuckets))
                {
                    histogramBuckets = new HistogramBucket[_bucketBoundaries.Length + 1];
                    _featureBuckets.Add(metricName, histogramBuckets);
                }

                var bucketIndex = GetBucketIndex(value);
                histogramBuckets[bucketIndex].Sum += value;
                histogramBuckets[bucketIndex].Count += 1;
            }

            int GetBucketIndex(double value)
            {
                for (var i = 0; i < _bucketBoundaries.Length; i++)
                {
                    if (value <= _bucketBoundaries[i])
                    {
                        return i;
                    }
                }

                return _bucketBoundaries.Length;
            }
        }

        public IDisposable LogBlockTimed(string featureName, string metricName)
        {
            return new TelemetryLogBlock(featureName, metricName, this);
        }

        public void PostTelemetry(TelemetrySession session)
        {
            lock (_lock)
            {
                foreach (var histogram in _histograms.Values)
                {
                    var telemetryEvent = new TelemetryEvent("vs/ide/vbcs/perf/" + _featureName);
                    var histogramEvent = new TelemetryHistogramEvent<int>(telemetryEvent, histogram);

                    session.PostMetricEvent(histogramEvent);
                }

                _histograms.Clear();

                if (_featureBuckets.Count > 0)
                {
                    // Try making one mega event for all histograms
                    var mergedTelemetryEvent = new TelemetryEvent("vs/ide/vbcs/perf/" + _featureName + "-merged");
                    foreach (var kvpFeatureBucket in _featureBuckets)
                    {
                        var histogramBucket = kvpFeatureBucket.Value;

                        var summaryCount = histogramBucket[0].Count;
                        var bucketCounts = summaryCount.ToString();

                        var summarySum = histogramBucket[0].Sum;
                        for (var i = 1; i < histogramBucket.Length; i++)
                        {
                            var bucketCount = histogramBucket[i].Count;
                            var bucketSum = histogramBucket[i].Sum;

                            summaryCount += bucketCount;
                            summarySum += bucketSum;

                            bucketCounts = bucketCounts + "," + bucketCount;
                        }

                        const string PropertyNamePrefix = "vs.ide.vbcs.perf.metric.";
                        const string BucketCountsSuffix = ".Bucket.Counts";
                        const string SummaryCountSuffix = ".Summary.Count";
                        const string SummarySumSuffix = ".Summary.Sum";

                        var requiredExtraSpace = PropertyNamePrefix.Length + Math.Max(Math.Max(BucketCountsSuffix.Length, SummaryCountSuffix.Length), SummarySumSuffix.Length);

                        var validPropertyName = EnsureValidTelemetryPropertyName(kvpFeatureBucket.Key, requiredExtraSpace);

                        var bucketCountsPropertyName = PropertyNamePrefix + validPropertyName + BucketCountsSuffix;
                        var summaryCountPropertyName = PropertyNamePrefix + validPropertyName + SummaryCountSuffix;
                        var summarySumPropertyName = PropertyNamePrefix + validPropertyName + SummarySumSuffix;

                        mergedTelemetryEvent.Properties[bucketCountsPropertyName] = bucketCounts;
                        mergedTelemetryEvent.Properties[summaryCountPropertyName] = summaryCount;
                        mergedTelemetryEvent.Properties[summarySumPropertyName] = summarySum;
                    }

                    mergedTelemetryEvent.Properties["vs.ide.vbcs.perf.MeterVersion"] = _meter.Version;
                    session.PostEvent(mergedTelemetryEvent);
                }

                _featureBuckets.Clear();
            }
        }

        /// <summary>
        /// Property name created by some extensions might have characters not permitted by telemetry API.
        /// The following are permitted in a property name: character, digit, _, /, \, . and -
        /// Invalid property name will get flagged as reserved.invalidevent.invalidpropertynames telemetry event. To avoid this issue,
        /// we will remove the non permissible characters from property name.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property name after stripping invalid characters</returns>
        private static string EnsureValidTelemetryPropertyName(string propertyName, int requiredExtraSpace)
        {
            var validPropertyName = propertyName;
            if (!validPropertyName.All(IsValidNameChar))
            {
                var sb = new StringBuilder(validPropertyName.Length);

                foreach (var c in validPropertyName)
                    sb.Append(IsValidNameChar(c) ? c : '_');

                validPropertyName = sb.ToString();
            }

            const int MaxPropertyNameLength = 150;

            if (validPropertyName.Length + requiredExtraSpace > MaxPropertyNameLength)
            {
                validPropertyName = validPropertyName[..(MaxPropertyNameLength - requiredExtraSpace)];
            }

            return validPropertyName;

            static bool IsValidNameChar(char c) => (char.IsLetterOrDigit(c) || c == '_' || c == '/' || c == '\\' || c == '.' || c == '-');
        }
    }
}
