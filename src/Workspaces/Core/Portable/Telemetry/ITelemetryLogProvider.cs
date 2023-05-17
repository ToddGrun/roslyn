// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal interface ITelemetryLogProvider
    {
        public ITelemetryLog? GetLog(FunctionId functionId);
        public ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries = null);
    }
}
