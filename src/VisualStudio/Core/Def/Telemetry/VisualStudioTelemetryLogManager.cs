// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal sealed class VisualStudioTelemetryLogManager
    {
        private readonly Dictionary<FunctionId, VisualStudioTelemetryLog> _logs;
        private readonly ILogger _telemetryLogger;
        private readonly object _lock;

        public VisualStudioTelemetryLogManager(ILogger telemetryLogger)
        {
            _telemetryLogger = telemetryLogger;
            _logs = new();
            _lock = new object();
        }

        public ITelemetryLog? GetLog(FunctionId functionId)
        {
            VisualStudioTelemetryLog? log;

            lock (_lock)
            {
                if (!_logs.TryGetValue(functionId, out log))
                {
                    log = new VisualStudioTelemetryLog(_telemetryLogger, functionId);
                    _logs.Add(functionId, log);
                }
            }

            return log;
        }
    }
}
