// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal static class TelemetryPerfEventNames
    {
        public const string SuggestedAction = nameof(SuggestedAction);
        public const string CodeRefactoring = nameof(CodeRefactoring);
        public const string CodeFixes = nameof(CodeFixes);
        public const string DiagnosticAnalyzers = nameof(DiagnosticAnalyzers);
    }
}
