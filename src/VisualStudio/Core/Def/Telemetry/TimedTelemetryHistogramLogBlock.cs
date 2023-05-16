// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class TimedTelemetryHistogramLogBlock : IDisposable
    {
        private readonly string _metricName;
        private readonly SharedStopwatch _stopwatch;
        private readonly TelemetryHistogramLogger _histogramLogger;

        public TimedTelemetryHistogramLogBlock(string metricName, TelemetryHistogramLogger histogramLogger)
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
