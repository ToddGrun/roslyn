// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        internal static readonly IReadOnlyDictionary<string, string> EmptyDictionary = new Dictionary<string, string>(KeyComparer);

        public static DictionaryAnalyzerConfigOptions Empty { get; } = new DictionaryAnalyzerConfigOptions(EmptyDictionary);

        // Note: Do not rename. Older versions of analyzers access this field via reflection.
        // https://github.com/dotnet/roslyn/blob/8e3d62a30b833631baaa4e84c5892298f16a8c9e/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Options/EditorConfig/EditorConfigStorageLocationExtensions.cs#L21
        private ImmutableDictionary<string, string>? _options;
        internal ImmutableDictionary<string, string> Options
        {
            get
            {
                if (_options is null)
                    _options = OptionsInternal.ToImmutableDictionary();

                return _options;
            }
        }

        internal IReadOnlyDictionary<string, string> OptionsInternal { get; }

        public DictionaryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
            => OptionsInternal = options;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => Options.TryGetValue(key, out value);

        public override IEnumerable<string> Keys
            => Options.Keys;
    }
}
