// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using BenchmarkDotNet.Attributes;

namespace IdeCoreBenchmarks
{
    public class BinarySearchBenchmarks
    {
        private static string[] s_toSearch = new[] {
            "Common.TypeName",
            "Common.TypeNameIdentifier",
            "Common.TypeNamespace",
            "Components.GenericTyped",
            "Components.NameMatch",
            "Runtime.Name"
        };

        [Params(
            "AAAAAAAA - NOT FOUND",
            "Common.TypeName",
            "Common.TypeName - NOT FOUND",
            "Common.TypeNameIdentifier",
            "Common.TypeNameIdentifier - NOT FOUND",
            "Common.TypeNamespace",
            "Common.TypeNamespace - NOT FOUND",
            "Components.GenericTyped",
            "Components.GenericTyped - NOT FOUND",
            "Components.NameMatch",
            "Components.NameMatch - NOT FOUND",
            "Runtime.Name",
            "Runtime.Name - NOT FOUND")]
        public string ToFind { get; set; }

        [Benchmark]
        public void NoComparerSpecified()
        {
            Array.BinarySearch(s_toSearch, ToFind);
        }

        [Benchmark]
        public void ComparerSpecified()
        {
            Array.BinarySearch(s_toSearch, ToFind, StringComparer.Ordinal);
        }
    }
}
