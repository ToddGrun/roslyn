// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed class RemoteWorkspaceTelemetryService : AbstractWorkspaceTelemetryService, IDisposable
    {
        private TelemetryLogger? _telemetryLogger;
        private TelemetrySession? _telemetrySession;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteWorkspaceTelemetryService(IThreadingContext threadingContext, IAsynchronousOperationListenerProvider asyncListenerProvider)
        {
            _threadingContext = threadingContext;
            _asyncListenerProvider = asyncListenerProvider;
        }

        public void Dispose()
        {
            if (_telemetryLogger is not null)
            {
                (_telemetryLogger as IDisposable)?.Dispose();
                _telemetryLogger = null;
            }

            if (_telemetrySession is not null)
            {
                _telemetrySession.Dispose();
                _telemetrySession = null;
            }
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta)
        {
            _telemetrySession = telemetrySession;
            _telemetryLogger = TelemetryLogger.Create(_telemetrySession, logDelta, _threadingContext, _asyncListenerProvider);

            return AggregateLogger.Create(
                _telemetryLogger,
                Logger.GetLogger());
        }
    }
}
