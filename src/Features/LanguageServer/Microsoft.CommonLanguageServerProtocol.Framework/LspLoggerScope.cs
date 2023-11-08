// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public abstract class LspLoggerScope : ILspLoggerScope
{
    private readonly Stopwatch _stopwatch;
    protected string Name { get; }
    protected List<(string, object?)> Properties { get; }
    protected long RequestDuration => _stopwatch.ElapsedMilliseconds;

    protected LspLoggerScope(string name, List<(string, object?)> properties)
    {
        _stopwatch = Stopwatch.StartNew();
        Name = name;
        Properties = properties;
    }

    public void AddProperty(string name, object? value)
    {
        Properties.Add(new(name, value));
    }

    public void AddProperties(ImmutableArray<(string, object?)> properties)
    {
        Properties.AddRange(properties);
    }

    public virtual void AddException(Exception exception, string? message = null, params object[] @params)
    {
        AddProperty("exception", exception.ToString());
    }

    public virtual void AddWarning(string message, params object[] @params)
    {
        AddProperty("warning", message);
    }

    public bool IsTimeThresholdExceeded() => RequestDuration > 1000;

    public virtual void Dispose()
    {
        _stopwatch.Stop();

        AddProperty("eventscope.method", Name);
        AddProperty("eventscope.ellapsedms", RequestDuration);
        AddProperty("eventscope.correlationid", Trace.CorrelationManager.ActivityId);
    }
}
