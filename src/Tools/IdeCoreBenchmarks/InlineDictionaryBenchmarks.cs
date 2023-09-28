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
    internal sealed class ReadOnlyMemoryOfCharComparerNew : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly ReadOnlyMemoryOfCharComparerNew Instance = new ReadOnlyMemoryOfCharComparerNew();

        private ReadOnlyMemoryOfCharComparerNew()
        {
        }

        //public static bool Equals(ReadOnlySpan<char> x, ReadOnlyMemory<char> y)
        //    => x.SequenceEqual(y.Span);

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            if (x.Length != y.Length) 
                return false;

            var xSpan = x.Span;
            var ySpan = y.Span;

            for (var i = 0; i < x.Length; i++)
            {
                if (xSpan[i] != ySpan[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
#if NET
            return string.GetHashCode(obj.Span);
#else
            return Hash.GetFNVHashCode(obj.Span);
#endif
        }
    }

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

        private InlineDictionary<ReadOnlyMemory<char>, int> _prebuiltInlineDictionaryNew;
        private Dictionary<ReadOnlyMemory<char>, int> _prebuiltDictionaryNew;

        private readonly int _loopCount = 10000000;

        [IterationSetup]
        public void IterationSetup()
        {
            _prebuiltInlineDictionary = new InlineDictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparer.Instance);
            _prebuiltDictionary = new Dictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparer.Instance);

            _prebuiltInlineDictionaryNew = new InlineDictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparerNew.Instance);
            _prebuiltDictionaryNew = new Dictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparerNew.Instance);

            for (var i = 0; i < Count; i++)
            {
                _prebuiltInlineDictionary.Add(_commonNames[i], 0);
                _prebuiltDictionary.Add(_commonNames[i], 0);

                _prebuiltInlineDictionaryNew.Add(_commonNames[i], 0);
                _prebuiltDictionaryNew.Add(_commonNames[i], 0);
            }
        }

        [Benchmark]
        public void Dictionary_CreateAndAdd()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                var dictionary = new Dictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparer.Instance);
                for (var i = 0; i < Count; i++)
                {
                    dictionary.Add(_commonNames[i], 0);
                }
            }
        }

        [Benchmark]
        public void Dictionary_CreateAndAddNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                var dictionary = new Dictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparerNew.Instance);
                for (var i = 0; i < Count; i++)
                {
                    dictionary.Add(_commonNames[i], 0);
                }
            }
        }

        [Benchmark]
        public void InlineDictionary_CreateAndAdd()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                var dictionary = new InlineDictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparer.Instance);
                for (var i = 0; i < Count; i++)
                {
                    dictionary.Add(_commonNames[i], 0);
                }
            }
        }

        [Benchmark]
        public void InlineDictionary_CreateAndAddNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                var dictionary = new InlineDictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryOfCharComparerNew.Instance);
                for (var i = 0; i < Count; i++)
                {
                    dictionary.Add(_commonNames[i], 0);
                }
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
        public void Dictionary_TryGetValue_FoundForAllNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (!_prebuiltDictionaryNew.TryGetValue(_commonNames[i], out _))
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
        public void InlineDictionary_TryGetValue_FoundForAllNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (!_prebuiltInlineDictionaryNew.TryGetValue(_commonNames[i], out _))
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
        public void Dictionary_TryGetValue_NotFoundNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (_prebuiltDictionaryNew.TryGetValue("NotInThere".AsMemory(), out _))
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

        [Benchmark]
        public void InlineDictionary_TryGetValue_NotFoundNew()
        {
            for (var loopIndex = 0; loopIndex < _loopCount; loopIndex++)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (_prebuiltInlineDictionaryNew.TryGetValue("NotInThere".AsMemory(), out _))
                        throw new Exception();
                }
            }
        }
    }
}
