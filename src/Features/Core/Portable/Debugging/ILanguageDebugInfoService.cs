﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Debugging;

internal interface ILanguageDebugInfoService : ILanguageService
{
    Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// Find an appropriate span to pass the debugger given a point in a snapshot.  Optionally pass back a string to
    /// pass to the debugger instead if no good span can be found.  For example, if the user hovers on "var" then we
    /// actually want to pass the fully qualified name of the type that 'var' binds to, to the debugger.
    /// </summary>
    /// <param name="includeKind">Whether or not the <see cref="DebugDataTipInfo.Kind"/> should be computed or not. Can
    /// be avoided from performance sensitive callers who do not need that information.</param>
    Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, bool includeKind, CancellationToken cancellationToken);
}
