// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal interface ITelemetryHistogramLoggerProvider
    {
        public ITelemetryHistogramLogger? GetLogger(FunctionId functionId, double[]? bucketBoundaries = null);
    }
}
