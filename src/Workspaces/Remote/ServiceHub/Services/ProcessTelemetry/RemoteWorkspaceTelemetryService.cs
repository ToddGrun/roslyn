// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed class RemoteWorkspaceTelemetryService : AbstractWorkspaceTelemetryService, IDisposable
    {
        private TelemetryLogger? _telemetryLogger;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteWorkspaceTelemetryService()
        {
        }

        public void Dispose()
        {
            (_telemetryLogger as IDisposable)?.Dispose();
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta)
        {
            _telemetryLogger = TelemetryLogger.Create(telemetrySession, logDelta);

            return AggregateLogger.Create(
                _telemetryLogger,
                Logger.GetLogger());
        }
    }
}
