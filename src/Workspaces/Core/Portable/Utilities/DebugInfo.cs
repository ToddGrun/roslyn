// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Utilities;

internal class DebugInfo : IDisposable
{
    private static readonly List<string> s_debugInfo = new();

    private readonly string _info;
    private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();
    private readonly long _elapsedAtStart;
    private static readonly Dictionary<string, int> s_counters = new();

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
        lock (s_debugInfo)
        {
            s_debugInfo.Add($"{info} : {s_stopwatch.ElapsedMilliseconds}");
        }
    }

    public static void AddCounter(string info)
    {
        lock (s_counters)
        {
            if (!s_counters.ContainsKey(info))
                s_counters[info]++;
            else
                s_counters[info] = 1;
        }
    }

    public void Dispose()
    {
        lock (s_debugInfo)
        {
            s_debugInfo.Add($"{_info} : ({_elapsedAtStart} - {s_stopwatch.ElapsedMilliseconds})");
        }
    }
}
