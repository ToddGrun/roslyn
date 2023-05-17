// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal static class TelemetryLogging
    {
        private static ITelemetryLogProvider? s_logProvider;

        public static void SetLogProvider(ITelemetryLogProvider logProvider)
        {
            s_logProvider = logProvider;
        }

        public static IDisposable? LogBlockTimeAggregated(FunctionId functionId, string metricName, int minThreshold = -1)
        {
            return GetAggregatingLog(functionId)?.LogBlockTimed(metricName, minThreshold);
        }

        public static void LogAggregated(FunctionId functionId, LogMessage logMessage)
        {
            GetAggregatingLog(functionId)?.Log(logMessage);
        }

        public static IDisposable? LogBlockTime(FunctionId functionId, string metricName, int minThreshold = -1)
        {
            return GetLog(functionId)?.LogBlockTimed(metricName, minThreshold);
        }

        public static void Log(FunctionId functionId, LogMessage logMessage)
        {
            GetLog(functionId)?.Log(logMessage);
        }

        public static ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            return s_logProvider?.GetAggregatingLog(functionId, bucketBoundaries);
        }

        public static ITelemetryLog? GetLog(FunctionId functionId)
        {
            return s_logProvider?.GetLog(functionId);
        }
    }
}
