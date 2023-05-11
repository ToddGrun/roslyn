// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal static class TelemetryHistogramLogger
    {
        private static ITelemetryHistogramLogger? s_logger;

        public static void SetLogger(ITelemetryHistogramLogger logger)
        {
            s_logger = logger;
        }

        public static IDisposable? LogBlockTimed(string featureName, string metricName)
        {
            return s_logger?.LogBlockTimed(featureName, metricName);
        }

        public static void Log(string featureName, string metricName, int value)
        {
            s_logger?.Log(featureName, metricName, value);
        }
    }
}
