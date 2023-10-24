// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Aggregate analyzer config options for a specific path.
/// </summary>
internal readonly struct AnalyzerConfigData(AnalyzerConfigOptionsResult result)
{
    public readonly StructuredAnalyzerConfigOptions ConfigOptions = StructuredAnalyzerConfigOptions.Create(result.GetAnalyzerOptions());
    public readonly IReadOnlyDictionary<string, string> AnalyzerOptions = result.GetAnalyzerOptions();
    public readonly IReadOnlyDictionary<string, ReportDiagnostic> TreeOptions = result.GetTreeOptions();
}
