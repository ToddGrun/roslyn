// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Provides a mechanism to log telemetry information containing the execution time between
    /// creation and disposal of this object.
    /// </summary>
    internal sealed class TimedTelemetryLogBlock(string name, int minThreshold, ITelemetryLog telemetryLog) : IDisposable
    {
#if !DEBUG
        private readonly string _name = name;
        private readonly int _minThreshold = minThreshold;
        private readonly ITelemetryLog _telemetryLog = telemetryLog;
        private readonly SharedStopwatch _stopwatch = SharedStopwatch.StartNew();

#endif

        public void Dispose()
        {
            // Don't add elapsed information in debug bits or while under debugger.
#if !DEBUG
            if (Debugger.IsAttached)
                return;

            var elapsed = (int)_stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed >= _minThreshold)
            {
                const string Name = nameof(Name);
                const string Value = nameof(Value);

                var logMessage = KeyValueLogMessage.Create(m =>
                {
                    m[Name] = _name;
                    m[Value] = elapsed;
                });

                _telemetryLog.Log(logMessage);
            }
#endif
        }
    }
}
