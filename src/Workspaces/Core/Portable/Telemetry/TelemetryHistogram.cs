// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal static class TelemetryHistogram
    {
        private static ITelemetryHistogramLoggerProvider? s_loggerProvider;

        public static void SetLoggerProvider(ITelemetryHistogramLoggerProvider loggerProvider)
        {
            s_loggerProvider = loggerProvider;
        }

        public static IDisposable? LogBlockTimed(FunctionId functionId, string metricName)
        {
            var logger = s_loggerProvider?.GetLogger(functionId);

            return logger?.LogBlockTimed(metricName);
        }

        public static void Log(FunctionId functionId, string metricName, int value)
        {
            var logger = s_loggerProvider?.GetLogger(functionId);

            logger?.Log(metricName, value);
        }

        public static ITelemetryHistogramLogger? GetLogger(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            return s_loggerProvider?.GetLogger(functionId, bucketBoundaries);
        }
    }
}
