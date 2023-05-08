// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal abstract class TelemetryHistogramLogger : ITelemetryHistogramLogger
    {
        private static ITelemetryHistogramLogger? s_logger;

        public static void SetLogger(ITelemetryHistogramLogger logger)
        {
            s_logger = logger;
        }

        public static IDisposable? LogBlockTimed(string featureName, string metricName)
        {
            return s_logger?.LogBlock(featureName, metricName);
        }

        public abstract IDisposable LogBlock(string featureName, string metricName);
    }
}
