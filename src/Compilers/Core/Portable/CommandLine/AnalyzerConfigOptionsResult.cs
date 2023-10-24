// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds results from <see cref="AnalyzerConfigSet.GetOptionsForSourcePath(string)"/>.
    /// </summary>
    /// TODO: Problems now since this isn't readonly?
    public struct AnalyzerConfigOptionsResult
    {
        private TreeOptions? _treeOptions;
        private AnalyzerOptions? _analyzerOptions;
        private readonly Dictionary<string, string> _analyzerOptionsInternal;
        private readonly Dictionary<string, ReportDiagnostic> _treeOptionsInternal;

        /// <summary>
        /// Options that customize diagnostic severity as reported by the compiler.
        /// </summary>
        public TreeOptions TreeOptions
        {
            get
            {
                _treeOptions = _treeOptions ?? _treeOptionsInternal.ToImmutableDictionary();
                return _treeOptions;
            }
        }

        /// <summary>
        /// Options that do not have any special compiler behavior and are passed to analyzers as-is.
        /// </summary>
        public AnalyzerOptions AnalyzerOptions
        {
            get
            {
                _analyzerOptions = _analyzerOptions ?? _analyzerOptionsInternal.ToImmutableDictionary();
                return _analyzerOptions;
            }
        }

        public IReadOnlyDictionary<string, ReportDiagnostic> GetTreeOptions() => _treeOptionsInternal;
        public IReadOnlyDictionary<string, string> GetAnalyzerOptions() => _analyzerOptionsInternal;

        /// <summary>
        /// Any produced diagnostics while applying analyzer configuration.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }


        internal AnalyzerConfigOptionsResult(
            Dictionary<string, ReportDiagnostic> treeOptions,
            Dictionary<string, string> analyzerOptions,
            ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(treeOptions != null);
            Debug.Assert(analyzerOptions != null);

            _treeOptionsInternal = treeOptions;
            _analyzerOptionsInternal = analyzerOptions;
            Diagnostics = diagnostics;
        }
    }
}
