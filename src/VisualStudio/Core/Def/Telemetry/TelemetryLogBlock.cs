// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class TelemetryLogBlock : IDisposable
    {
        private static readonly AsyncLocal<TelemetryLogBlock?> s_context = new();

        private readonly string _metricName;
        private readonly string _featureName;
        private readonly SharedStopwatch _stopwatch;
        private readonly TelemetryHistogramLoggerForFeature _histogramLogger;
        private readonly TelemetryLogBlock? _priorBlock;

        public TelemetryLogBlock(string featureName, string metricName, TelemetryHistogramLoggerForFeature histogramLogger)
        {
            _featureName = featureName;
            _metricName = metricName;
            _histogramLogger = histogramLogger;
            _stopwatch = SharedStopwatch.StartNew();

            _priorBlock = s_context.Value;
            s_context.Value = this;

            var priorBlock = _priorBlock;
            while (priorBlock is not null)
            {
                if (priorBlock._histogramLogger == _histogramLogger)
                {
                    _metricName = priorBlock._metricName + "." + _metricName;
                    break;
                }

                priorBlock = priorBlock._priorBlock;
            }
        }

        public void Dispose()
        {
            var elapsed = (int)_stopwatch.Elapsed.TotalMilliseconds;
            _histogramLogger.Log(_featureName, _metricName, elapsed);

            s_context.Value = _priorBlock;
        }
    }
}
