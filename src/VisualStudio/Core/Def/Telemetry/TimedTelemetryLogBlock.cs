// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal class TimedTelemetryLogBlock : IDisposable
    {
        private readonly string _name;
        private readonly SharedStopwatch _stopwatch;
        private readonly int _minThreshold;
        private readonly ITelemetryLog _telemetryLog;

        public TimedTelemetryLogBlock(string name, int minThreshold, ITelemetryLog telemetryLog)
        {
            _name = name;
            _minThreshold = minThreshold;
            _telemetryLog = telemetryLog;
            _stopwatch = SharedStopwatch.StartNew();
        }

        public void Dispose()
        {
            var elapsed = (int)_stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed >= _minThreshold)
            {
                var logMessage = KeyValueLogMessage.Create(LogType.Trace, m =>
                {
                    m["Name"] = _name;
                    m["Delay"] = elapsed;
                });

                _telemetryLog.Log(logMessage);
            }
        }
    }
}
