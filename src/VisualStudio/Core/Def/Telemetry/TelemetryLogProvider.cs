// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal class TelemetryLogProvider : ITelemetryLogProvider, IDisposable
    {
        private readonly AggregatingTelemetryLogManager _aggregatingTelemetryLogManager;
        private readonly VisualStudioTelemetryLogManager _visualStudioTelemetryLogManager;

        private TelemetryLogProvider(TelemetrySession session, ILogger telemetryLogger)
        {
            _aggregatingTelemetryLogManager = new AggregatingTelemetryLogManager(session);
            _visualStudioTelemetryLogManager = new VisualStudioTelemetryLogManager(telemetryLogger);
        }

        public static TelemetryLogProvider Create(TelemetrySession session, ILogger telemetryLogger)
        {
            var logProvider = new TelemetryLogProvider(session, telemetryLogger);

            TelemetryLogging.SetLogProvider(logProvider);

            return logProvider;
        }

        public void Dispose()
        {
            _aggregatingTelemetryLogManager.Dispose();
        }

        public ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            return _aggregatingTelemetryLogManager.GetLog(functionId, bucketBoundaries);
        }

        public ITelemetryLog? GetLog(FunctionId functionId)
        {
            return _visualStudioTelemetryLogManager.GetLog(functionId);
        }
    }
}
