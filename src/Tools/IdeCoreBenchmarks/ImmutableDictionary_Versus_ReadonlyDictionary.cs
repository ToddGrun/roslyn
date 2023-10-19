// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class ImmutableDictionary_Versus_ReadonlyDictionary
    {
        private static KeyValuePair<string, int>[][] s_data;

        private static string[][] s_strings;

        private int s_count = 10000;

        [Params(1, 5, 10, 20, 100)]
        public int CollectionSize
        {
            get;
            set;
        }

        private string[] GetStringArray()
        {
            switch (CollectionSize)
            {
                case 1:
                    return s_strings[0];
                case 5:
                    return s_strings[1];
                case 10:
                    return s_strings[2];
                case 20:
                    return s_strings[3];
                default:
                    return s_strings[4];
            }
        }

        private KeyValuePair<string, int>[] GetDataArray()
        {
            switch (CollectionSize)
            {
                case 1:
                    return s_data[0];
                case 5:
                    return s_data[1];
                case 10:
                    return s_data[2];
                case 20:
                    return s_data[3];
                default:
                    return s_data[4];
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var sizes = new int[] { 1, 5, 10, 20, 100 };

            s_strings = new string[sizes.Length][];
            s_data = new KeyValuePair<string, int>[sizes.Length][];

            for (var i = 0; i < sizes.Length; i++)
            {
                var size = sizes[i];
                var curData = new KeyValuePair<string, int>[size]; ;
                var curString = new string[size];

                s_data[i] = curData;
                s_strings[i] = curString;

                for (var j = 0; j < size; j++)
                {
                    curString[j] = "string" + j;
                    curData[j] = new KeyValuePair<string, int>(curString[j], j);
                }
            }
        }

        [Benchmark]
        public void ImmutableDictionary_Creation()
        {
            var dataArray = GetDataArray();

            for (var loopIndex = 0; loopIndex < s_count; loopIndex++)
            {
                var foo = ImmutableDictionary<string, int>.Empty.ToBuilder();
                foo.AddRange(dataArray);

                var dict = foo.ToImmutableDictionary();
            }
        }

        [Benchmark]
        public void ReadOnlyDictionary_Creation()
        {
            var dataArray = GetDataArray();

            for (var loopIndex = 0; loopIndex < s_count; loopIndex++)
            {
                var foo = new Dictionary<string, int>(dataArray.Length);
                foo.AddRange(dataArray);

                var dict = new ReadOnlyDictionary<string, int>(foo);
            }
        }

        [Benchmark]
        public void ImmutableDictionary_IndexAll()
        {
            var dataArray = GetDataArray();
            var stringArray = GetStringArray();

            var foo = ImmutableDictionary<string, int>.Empty.ToBuilder();
            foo.AddRange(dataArray);

            for (var loopIndex = 0; loopIndex < s_count; loopIndex++)
            {
                for (var i = 0; i < dataArray.Length; i++)
                {
                    if (!foo.ContainsKey(stringArray[i]))
                        throw new Exception();
                }
            }
        }

        [Benchmark]
        public void ReadOnlyDictionary_IndexAll()
        {
            var dataArray = GetDataArray();
            var stringArray = GetStringArray();

            var dict = new Dictionary<string, int>(dataArray.Length);
            dict.AddRange(dataArray);

            var foo = new ReadOnlyDictionary<string, int>(dict);

            for (var loopIndex = 0; loopIndex < s_count; loopIndex++)
            {
                for (var i = 0; i < dataArray.Length; i++)
                {
                    if (!foo.ContainsKey(stringArray[i]))
                        throw new Exception();
                }
            }
        }
    }
}
