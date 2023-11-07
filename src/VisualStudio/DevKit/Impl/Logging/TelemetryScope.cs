// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.VisualStudio.LanguageServices.DevKit.Logging;

internal sealed class TelemetryScope : ILspLoggerScope
{
    private readonly string _name;
    private readonly ITelemetryReporter? _telemetryReporter;
    private readonly ArrayBuilder<(string, object?)> _properties;
    private readonly Stopwatch _stopwatch;

    public TelemetryScope(string name, ITelemetryReporter? telemetryReporter)
    {
        _name = name;
        _stopwatch = Stopwatch.StartNew();
        _telemetryReporter = telemetryReporter;
        _properties = ArrayBuilder<(string, object?)>.GetInstance();
    }

    public void AddProperty(string name, object? value)
    {
        _properties.Add(new(name, value));
    }

    public void AddProperties(ImmutableArray<(string, object?)> properties)
    {
        _properties.AddRange(properties);
    }

    public void AddException(Exception exception, string? message = null, params object[] @params)
    {
        AddProperty("exception", exception.ToString());
    }

    public void AddWarning(string message, params object[] @params)
    {
        AddProperty("warning", message);
    }

    public void Dispose()
    {
        AddProperty("eventscope.ellapsedms", _stopwatch.Elapsed.Milliseconds);

        _telemetryReporter?.Log(_name, _properties.ToImmutableAndFree());

        // TODO: aggregated telemetry?
    }
}
