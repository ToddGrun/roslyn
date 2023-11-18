// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LogHub;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal class LogHubLspLogger : ILspServiceLogger
    {
        private readonly TraceConfiguration _configuration;
        private readonly TraceSource _traceSource;
        private bool _disposed;

        public LogHubLspLogger(TraceConfiguration configuration, TraceSource traceSource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                Contract.Fail($"{GetType().FullName} was double disposed");
                return;
            }

            _disposed = true;
            _traceSource.Flush();
            _traceSource.Close();
            _configuration.Dispose();
        }

        public void LogInformation(string message, params object[] @params)
        {
            // Explicitly call TraceEvent here instead of TraceInformation.
            // TraceInformation indirectly calls string.Format which throws if the message
            // has unescaped curlies in it (can be a part of a URI for example).
            // Since we have no need to call string.Format here, we don't.
            _traceSource.TraceEvent(TraceEventType.Information, id: 0, message);
        }

        public void LogWarning(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Warning, id: 0, message);
        }

        public void LogError(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Error, id: 0, message);
        }

        public void LogException(Exception exception, string? message = null, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Error, id: 0, "Exception: {0}", exception);
        }

        public void LogStartContext(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Start, id: 0, message);
        }

        public void LogEndContext(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Stop, id: 0, message);
        }

        public ILspLoggerScope BeginScope(string message)
        {
            return new LspTelemetryScope(message, this);
        }

        internal sealed class LspTelemetryScope : LspLoggerScope
        {
            private readonly ILspServiceLogger _hostLogger;
            private static readonly ObjectPool<List<KeyValuePair<string, object?>>> s_pool = new ObjectPool<List<KeyValuePair<string, object?>>>(() => new(), trimOnFree: true);
            private readonly IDisposable? _telemetryBlockDisposer;

            public LspTelemetryScope(string name, ILspServiceLogger hostLogger)
                : base(name, s_pool.Allocate())
            {
                _hostLogger = hostLogger;
                _hostLogger.LogStartContext(Name);

                var message = KeyValueLogMessage.Create(LogType.Trace, m =>
                {
                    foreach (var (name, value) in Properties)
                    {
                        m[name] = value;
                    }
                });

                _telemetryBlockDisposer = TelemetryLogging.LogBlockTime(FunctionId.LSP_RequestDuration, message, minThresholdMs: (int)DurationThreshold);
            }

            public override void AddException(Exception exception, string? message = null, params object[] @params)
            {
                base.AddException(exception, message, @params);

                _hostLogger.LogException(exception, message, @params);
            }

            public override void AddWarning(string message, params object[] @params)
            {
                base.AddWarning(message, @params);

                _hostLogger.LogWarning(message, @params);
            }

            public override void Dispose()
            {
                base.Dispose();

                // Fire the telemetry event if the timing threshold is exceeded
                _telemetryBlockDisposer?.Dispose();

                _hostLogger.LogEndContext(Name);

                // Add aggregated telemetry information
                TelemetryLogging.LogAggregated(FunctionId.LSP_RequestDuration, $"{Name}", (int)RequestDuration);

                s_pool.Free(Properties);
            }
        }
    }
}
