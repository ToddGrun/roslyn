// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Provides access to posting telemetry events or adding information
    /// to aggregated telemetry events.
    /// </summary>
    internal static class TelemetryLogging
    {
        private static ITelemetryLogProvider? s_logProvider;

        public static void SetLogProvider(ITelemetryLogProvider logProvider)
        {
            s_logProvider = logProvider;
        }

        /// <summary>
        /// Posts a telemetry event representing the <paramref name="functionId"/> operation with context message <paramref name="logMessage"/>
        /// </summary>
        public static void Log(FunctionId functionId, LogMessage logMessage)
        {
            GetLog(functionId)?.Log(logMessage);
        }

        /// <summary>
        /// Posts a telemetry event representing the <paramref name="functionId"/> operation 
        /// only if the block duration meets or exceeds <paramref name="minThreshold"/> milliseconds.
        /// This event will contain properties from which both <paramref name="name"/> and <paramref name="minThreshold"/>.
        /// and block execution time can be determined.
        /// </summary>
        /// <param name="minThreshold">Optional parameter used to determine whether to send the telemetry event</param>
        public static IDisposable? LogBlockTime(FunctionId functionId, string name, int minThreshold = -1)
        {
            return GetLog(functionId)?.LogBlockTime(name, minThreshold);
        }

        /// <summary>
        /// Adds information to an aggregated telemetry event representing the <paramref name="functionId"/> operation 
        /// with the specified name and value.
        /// </summary>
        public static void LogAggregated(FunctionId functionId, string name, int value)
        {
            const string Name = nameof(Name);
            const string Value = nameof(Value);

            var logMessage = KeyValueLogMessage.Create(m =>
            {
                m[Name] = name;
                m[Value] = value;
            });

            GetAggregatingLog(functionId)?.Log(logMessage);
        }

        /// <summary>
        /// Adds block execution time to an aggregated telemetry event representing the <paramref name="functionId"/> operation 
        /// with metric <paramref name="metricName"/> only if the block duration meets or exceeds <paramref name="minThreshold"/> milliseconds.
        /// </summary>
        /// <param name="minThreshold">Optional parameter used to determine whether to send the telemetry event</param>
        public static IDisposable? LogBlockTimeAggregated(FunctionId functionId, string metricName, int minThreshold = -1)
        {
            return GetAggregatingLog(functionId)?.LogBlockTime(metricName, minThreshold);
        }

        /// <summary>
        /// Returns non-aggregating telemetry log.
        /// </summary>
        public static ITelemetryLog? GetLog(FunctionId functionId)
        {
            return s_logProvider?.GetLog(functionId);
        }

        /// <summary>
        /// Returns aggregating telemetry log.
        /// </summary>
        public static ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries = null)
        {
            return s_logProvider?.GetAggregatingLog(functionId, bucketBoundaries);
        }
    }
}
