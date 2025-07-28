// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SmallDictionaryBenchmark
    {
        [Params(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 20, 100)]
        public int Count { get; set; }

        private const int LoopCount = 10_000;

        private string[] _keys = null!;
        private object[] _objectKeys = null!;

        [IterationSetup]
        public void IterationSetup()
        {
            _keys = new string[Count];
            _objectKeys = new object[Count];

            for (var i = 0; i < Count; i++)
            {
                _keys[i] = "string - " + i;
                _objectKeys[i] = i * 3;
            }
        }

        //[Benchmark]
        public void SmallDictionary()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                // Create the collection
                var collection = new SmallDictionary<string, int>();

                // Add properties to the collection
                foreach (var key in _keys)
                {
                    collection.Add(key, 1);
                }

                // Query the collection
                foreach (var key in _keys)
                {
                    _ = collection[key];
                }

                // Enumerate/query the collection
                foreach (var key in collection.Keys)
                {
                    _ = collection.ContainsKey(key);
                }
            }
        }

        //[Benchmark]
        public void HybridDictionary()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                // Create the collection
                var collection = new HybridDictionary(_objectKeys.Length);

                // Add properties to the collection
                foreach (var key in _objectKeys)
                {
                    collection.Add(key, "value");
                }

                // Query the collection
                foreach (var key in _objectKeys)
                {
                    _ = collection[key];
                }

                // Enumerate/query the collection
                foreach (DictionaryEntry c in collection)
                {
                    _ = collection.Contains(c.Key);
                }
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                // Create the collection
                var collection = new Dictionary<string, int>(_keys.Length);

                // Add properties to the collection
                foreach (var key in _keys)
                {
                    collection.Add(key, 1);
                }

                // Query the collection
                foreach (var key in _keys)
                {
                    _ = collection[key];
                }

                // Enumerate/query the collection
                foreach (var c in collection)
                {
                    _ = collection.ContainsKey(c.Key);
                }
            }
        }

        [Benchmark]
        public void PooledDictionary()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                // Create the collection
                using var _1 = PooledDictionary<string, int>.GetInstance(out var collection);

                // Add properties to the collection
                foreach (var key in _keys)
                {
                    collection.Add(key, 1);
                }

                // Query the collection
                foreach (var key in _keys)
                {
                    _ = collection[key];
                }

                // Enumerate/query the collection
                foreach (var c in collection)
                {
                    _ = collection.ContainsKey(c.Key);
                }
            }
        }

        [Benchmark]
        public void PooledHybridDictionary()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                // Create the collection
                using var _1 = PooledHybridDictionary<string, int>.GetInstance(out var collection);

                // Add properties to the collection
                foreach (var key in _keys)
                {
                    collection.Add(key, 1);
                }

                // Query the collection
                foreach (var key in _keys)
                {
                    _ = collection[key];
                }

                // Enumerate/query the collection
                foreach (var c in collection)
                {
                    _ = collection.ContainsKey(c.Key);
                }
            }
        }
    }
}
