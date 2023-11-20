// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.VisualStudio.LanguageServices.DevKit.Logging;

internal sealed class LspTelemetryScope : LspLoggerScope
{
    private readonly ITelemetryReporter _telemetryReporter;
    private static readonly ObjectPool<List<KeyValuePair<string, object?>>> s_pool = new ObjectPool<List<KeyValuePair<string, object?>>>(() => new(), trimOnFree: true);

    public LspTelemetryScope(string name, ITelemetryReporter telemetryReporter)
        : base(name, s_pool.Allocate())
    {
        _telemetryReporter = telemetryReporter;
    }

    public override void Dispose()
    {
        base.Dispose();

        if (RequestDuration >= DurationThreshold)
            _telemetryReporter.Log(Name, Properties);

        // TODO: aggregated telemetry?

        s_pool.Free(Properties);
    }
}
