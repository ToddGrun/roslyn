// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticOptionsStorage
    {
        public static readonly Option2<bool> LspPullDiagnosticsFeatureFlag = new(
            "dotnet_enable_lsp_pull_diagnostics", defaultValue: false);

        public static readonly Option2<bool> LogTelemetryForBackgroundAnalyzerExecution = new(
            "dotnet_log_telemetry_for_background_analyzer_execution", defaultValue: false);

        public static readonly Option2<bool> LightbulbSkipExecutingDeprioritizedAnalyzers = new(
            "dotnet_lightbulb_skip_executing_deprioritized_analyzers", defaultValue: false);

        // TODO: Figure out how this works and move to a telemetry specific one?
        public static readonly Option2<int> LogTelemetryForPerformAnalysisMsThreshold = new(
            "dotnet_log_telemetry_for_perform_analysis_ms_threshold", defaultValue: 250);
    }
}
