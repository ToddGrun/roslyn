// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal abstract class AbstractWorkspaceTelemetryService : IWorkspaceTelemetryService
    {
        public TelemetrySession? CurrentSession { get; private set; }

        protected abstract ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta);

        private readonly VSTelemetryMeterProvider _meterProvider = new VSTelemetryMeterProvider();

        public void InitializeTelemetrySession(TelemetrySession telemetrySession, bool logDelta)
        {
            Contract.ThrowIfFalse(CurrentSession is null);

            Logger.SetLogger(CreateLogger(telemetrySession, logDelta));
            FaultReporter.RegisterTelemetrySesssion(telemetrySession);

            CurrentSession = telemetrySession;

            TelemetrySessionInitialized();
        }

        protected virtual void TelemetrySessionInitialized()
        {
        }

        public bool HasActiveSession
            => CurrentSession != null && CurrentSession.IsOptedIn;

        public string? SerializeCurrentSessionSettings()
            => CurrentSession?.SerializeSettings();

        public void RegisterUnexpectedExceptionLogger(TraceSource logger)
            => FaultReporter.RegisterLogger(logger);

        public void UnregisterUnexpectedExceptionLogger(TraceSource logger)
            => FaultReporter.UnregisterLogger(logger);

        public ITelemetryMeter CreateMeter(string name, string? version = null)
        {
            return new TelemetryMeter(_meterProvider.CreateMeter(name, version));
        }
    }

    internal class TelemetryMeter : ITelemetryMeter
    {
        private readonly IMeter _meter;

        public TelemetryMeter(IMeter meter)
        {
            _meter = meter;
        }

        public ITelemetryHistogram<TType> CreateHistogram<TType>(string name, string? unit = null, string? description = null) where TType : struct
        {
            return new TelemetryHistogram<TType>(_meter.CreateHistogram<TType>(name, unit, description));
        }

        public void Dispose()
        {
            _meter.Dispose();
        }
    }

    internal class TelemetryHistogram<TType> : ITelemetryHistogram<TType> where TType : struct
    {
        private readonly IHistogram<TType> _histogram;

        public TelemetryHistogram(IHistogram<TType> histogram)
        {
            _histogram = histogram;
        }

        public void Record(TType value, KeyValuePair<string, object?> tag)
        {
            _histogram.Record(value, tag);
        }
    }
}
