// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    internal class DictionaryOrArrayBenchmarks
    {
        private static int[] _sizes = new int[] { 0, 1, 2, 3, 4, 5, 6, 10, 100 };

        [Params(0, 1, 2, 3, 4, 5, 6, 10, 100)]
        public int Count { get; set; }

        private List<(string, string)>[] _lists;
        private Dictionary<string, string>[] _dictionaries;

        private int _sizeIndex;
        private string _firstValue;
        private string _lastValue;

        [IterationSetup]
        public void IterationSetup()
        {
            _firstValue = "test1";
            _lastValue = "test" + Count.ToString();

            _sizeIndex = _sizes.IndexOf(val => val == Count);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _lists = new List<(string, string)>[_sizes.Length];
            _dictionaries = new Dictionary<string, string>[_sizes.Length];
            for (var i = 1; i <= _sizes.Length; i++)
            {
                var size = _sizes[i];

                var list = new List<(string, string)>(size);
                var dictionary = new Dictionary<string, string>(size);

                for (var j = 0; j < size; j++)
                {
                    var str = "test" + j.ToString();
                    list.Add((str, str));
                    dictionary[str] = str;
                }

                _lists[i] = list;
                _dictionaries[i] = dictionary;
            }
        }

        [Benchmark]
        public void TryGetValue_Array_First()
        {
            var list = _lists[_sizeIndex];
            for (var i = 0; i < 100; i++)
            {
                list.IndexOf()
            }
        }

        [Benchmark]
        public void TryGetValue_Array_Last()
        {
        }

        [Benchmark]
        public void TryGetValue_Array_NotFound()
        {
        }

        [Benchmark]
        public void TryGetValue_Dictionary_First()
        {
            var dictionary = _dictionaries[_sizeIndex];
            for (var i = 0; i < 100; i++)
            {
                dictionary.TryGetValue(_firstValue, out _);
            }
        }

        [Benchmark]
        public void TryGetValue_Dictionary_Last()
        {
            var dictionary = _dictionaries[_sizeIndex];
            for (var i = 0; i < 100; i++)
            {
                dictionary.TryGetValue(_lastValue, out _);
            }
        }

        [Benchmark]
        public void TryGetValue_Dictionary_NotFound()
        {
            var dictionary = _dictionaries[_sizeIndex];
            for (var i = 0; i < 100; i++)
            {
                dictionary.TryGetValue("test not found", out _);
            }
        }
    }
}
