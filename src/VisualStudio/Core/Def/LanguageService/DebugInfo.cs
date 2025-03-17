// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal class DebugInfo : IDisposable
{
    private static readonly List<string> s_debugInfo = new();

    private readonly string _info;
    private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();
    private readonly long _elapsedAtStart;

    private DebugInfo(string info)
    {
        _info = info;
        _elapsedAtStart = s_stopwatch.ElapsedMilliseconds;
    }

    public static IDisposable AddScopedInfo(string info)
    {
        return new DebugInfo(info);
    }

    public static void AddInfo(string info)
    {
        s_debugInfo.Add($"{info} : {s_stopwatch.ElapsedMilliseconds}");
    }

    public void Dispose()
    {
        s_debugInfo.Add($"{_info} : ({_elapsedAtStart} - {s_stopwatch.ElapsedMilliseconds})");
    }
}
