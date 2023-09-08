// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    /// <summary>
    /// Provides access to an appropriate <see cref="ITelemetryLogProvider"/> for logging telemetry.
    /// </summary>
    internal sealed class TelemetryLogProvider : ITelemetryLogProvider
    {
        private readonly AggregatingTelemetryLogManager _aggregatingTelemetryLogManager;
        private readonly TelemetrySession _session;

        private readonly ILogger _telemetryLogger;
        private ImmutableDictionary<FunctionId, TelemetryLog> _logs = ImmutableDictionary<FunctionId, TelemetryLog>.Empty;

        private TelemetryLogProvider(TelemetrySession session, ILogger telemetryLogger, IAsynchronousOperationListener asyncListener)
        {
            _session = session;
            _telemetryLogger = telemetryLogger;

            _aggregatingTelemetryLogManager = new AggregatingTelemetryLogManager(this, asyncListener);
        }

        public static TelemetryLogProvider Create(TelemetrySession session, ILogger telemetryLogger, IAsynchronousOperationListener asyncListener)
        {
            var logProvider = new TelemetryLogProvider(session, telemetryLogger, asyncListener);

            TelemetryLogging.SetLogProvider(logProvider);

            return logProvider;
        }

        public bool IsOptedIn => _session.IsOptedIn;

        /// <summary>
        /// Returns an <see cref="ITelemetryLog"/> for logging telemetry.
        /// </summary>
        public ITelemetryLog? GetLog(FunctionId functionId)
        {
            if (!IsOptedIn)
                return null;

            return ImmutableInterlocked.GetOrAdd(ref _logs, functionId, functionId => new TelemetryLog(_telemetryLogger, functionId));
        }

        /// <summary>
        /// Returns an aggregating <see cref="ITelemetryLog"/> for logging telemetry.
        /// </summary>
        public ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            return _aggregatingTelemetryLogManager.GetLog(functionId, bucketBoundaries);
        }

        public ITelemetryLog CreateAggregatingLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            return new AggregatingTelemetryLog(_session, functionId, bucketBoundaries, this);
        }

        public void PostTelemetry(ITelemetryLog telemetryLog)
        {
        }
    }
}
