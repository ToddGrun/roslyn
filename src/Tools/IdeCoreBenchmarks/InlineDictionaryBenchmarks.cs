// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class InlineDictionaryBenchmarks
    {
        [Params(0, 1, 2, 3, 4, 5, 6, 10)]
        public int Count { get; set; }

        private readonly ReadOnlyMemory<char>[] _commonNames = new[] {
            "System".AsMemory(), "Microsoft".AsMemory(), "Collection".AsMemory(), "Immutable".AsMemory(), "CodeAnalysis".AsMemory(),
            "CSharp".AsMemory(), "VisualBasic".AsMemory(), "Completion".AsMemory(), "Compilation".AsMemory(), "Text".AsMemory() };

        private InlineDictionary<ReadOnlyMemory<char>, int> _prebuiltInlineDictionary;
        private Dictionary<ReadOnlyMemory<char>, int> _prebuiltDictionary;

        private readonly int _loopCount = 10000000;

        [IterationSetup]
        public void IterationSetup()
        {
            _prebuiltDictionary = new(Count, ReadOnlyMemoryOfCharComparer.Instance);

            var list = new List<KeyValuePair<ReadOnlyMemory<char>, int>>(Count);
            for (var i = 0; i < Count; i++)
            {
                list.Add(new KeyValuePair<ReadOnlyMemory<char>, int>(_commonNames[i], 0));
                _prebuiltDictionary.Add(_commonNames[i], 0);
            }

            _prebuiltInlineDictionary = InlineDictionary<ReadOnlyMemory<char>, int>.Create(list, ReadOnlyMemoryOfCharComparer.Instance);
        }

        [Benchmark]
        public void Dictionary_CreateAndAdd()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                var dictionary = new Dictionary<ReadOnlyMemory<char>, int>(Count, ReadOnlyMemoryOfCharComparer.Instance);
                for (var i = 0; i < Count; i++)
                {
                    dictionary.Add(_commonNames[i], 0);
                }
            }
        }

        [Benchmark]
        public void InlineDictionary_CreateAndAdd()
        {
            var list = new List<KeyValuePair<ReadOnlyMemory<char>, int>>(Count);
            for (var i = 0; i < Count; i++)
            {
                list.Add(new KeyValuePair<ReadOnlyMemory<char>, int>(_commonNames[i], 0));
            }

            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                _ = InlineDictionary<ReadOnlyMemory<char>, int>.Create(list, ReadOnlyMemoryOfCharComparer.Instance);
            }
        }

        [Benchmark]
        public void Dictionary_TryGetValue_FoundForAll()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (!_prebuiltDictionary.TryGetValue(_commonNames[i], out _))
                        throw new Exception();
                }
            }
        }

        [Benchmark]
        public void InlineDictionary_TryGetValue_FoundForAll()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (!_prebuiltInlineDictionary.TryGetValue(_commonNames[i], out _))
                        throw new Exception();
                }
            }
        }

        [Benchmark]
        public void Dictionary_TryGetValue_NotFound()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (_prebuiltDictionary.TryGetValue("NotInThere".AsMemory(), out _))
                        throw new Exception();
                }
            }
        }

        [Benchmark]
        public void InlineDictionary_TryGetValue_NotFound()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (_prebuiltInlineDictionary.TryGetValue("NotInThere".AsMemory(), out _))
                        throw new Exception();
                }
            }
        }
    }
}
