// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal class InlineDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly IEqualityComparer<TKey> _comparer;

        private KeyValuePair<TKey, TValue> _item0;
        private KeyValuePair<TKey, TValue> _item1;
        private KeyValuePair<TKey, TValue> _item2;
        private KeyValuePair<TKey, TValue> _item3;

        private Dictionary<TKey, TValue>? _dictionary;

        private int _count;

        public InlineDictionary(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
        }

        public InlineDictionary(int count, IEqualityComparer<TKey> comparer)
        {
            if (count > 4)
                _dictionary = new Dictionary<TKey, TValue>(count, comparer);

            _comparer = comparer;
        }

        public void Add(TKey key, TValue value)
        {
            if (_dictionary is not null)
            {
                _dictionary.Add(key, value);
                return;
            }

            _count++;
            switch (_count)
            {
                case 1:
                    _item0 = new KeyValuePair<TKey, TValue>(key, value);
                    break;
                case 2:
                    _item1 = new KeyValuePair<TKey, TValue>(key, value);
                    break;
                case 3:
                    _item2 = new KeyValuePair<TKey, TValue>(key, value);
                    break;
                case 4:
                    _item3 = new KeyValuePair<TKey, TValue>(key, value);
                    break;
                default:
                    MoveInlineToDictionary();
                    _dictionary.Add(key, value);
                    break;
            };
        }

        [MemberNotNull(nameof(_dictionary))]
        private void MoveInlineToDictionary()
        {
            Debug.Assert(_dictionary is null);

            var dictionary = new Dictionary<TKey, TValue>();

            dictionary.Add(_item0.Key, _item0.Value);
            dictionary.Add(_item1.Key, _item1.Value);
            dictionary.Add(_item2.Key, _item2.Value);
            dictionary.Add(_item3.Key, _item3.Value);

            _item0 = default;
            _item1 = default;
            _item2 = default;
            _item3 = default;

            _count = 0;
            _dictionary = dictionary;
        }

        public int Count => _dictionary?.Count ?? _count;

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            if (_dictionary is not null)
                return _dictionary.TryGetValue(key, out value);

            if (_count == 0)
            {
                value = default;
                return false;
            }

            if (_comparer.Equals(key, _item0.Key))
            {
                value = _item0.Value;
                return true;
            }

            if (_count == 1)
            {
                value = default;
                return false;
            }

            if (_comparer.Equals(key, _item1.Key))
            {
                value = _item1.Value;
                return true;
            }

            if (_count == 2)
            {
                value = default;
                return false;
            }

            if (_comparer.Equals(key, _item2.Key))
            {
                value = _item2.Value;
                return true;
            }

            if (_count == 3)
            {
                value = default;
                return false;
            }

            if (_comparer.Equals(key, _item3.Key))
            {
                value = _item3.Value;
                return true;
            }

            value = default;
            return false;
        }

        internal static InlineDictionary<TKey, ImmutableArray<TValue>> FromArrayBuilder(ArrayBuilder<TValue> builder, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (builder.Count == 1)
            {
                var dictionary1 = new InlineDictionary<TKey, ImmutableArray<TValue>>(comparer);
                var value = builder[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (builder.Count == 0)
            {
                return new InlineDictionary<TKey, ImmutableArray<TValue>>(comparer);
            }

            // bucketize
            // prevent reallocation. it may not have 'count' entries, but it won't have more. 
            var accumulator = new Dictionary<TKey, ArrayBuilder<TValue>>(builder.Count, comparer);
            for (var i = 0; i < builder.Count; i++)
            {
                var item = builder[i];
                var key = keySelector(item);
                if (!accumulator.TryGetValue(key, out var bucket))
                {
                    bucket = ArrayBuilder<TValue>.GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new InlineDictionary<TKey, ImmutableArray<TValue>>(accumulator.Count, comparer);

            // freeze
            foreach (var pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        [NonCopyable]
        public struct Enumerator
        {
            private readonly InlineDictionary<TKey, TValue> _parent;
            private readonly Dictionary<TKey, TValue>.Enumerator _dictionaryEnumerator;

            private KeyValuePair<TKey, TValue> _current;
            private int _nextIndex;

            public Enumerator(InlineDictionary<TKey, TValue> parent)
            {
                _parent = parent;
                if (parent._dictionary is not null)
                    _dictionaryEnumerator = parent._dictionary.GetEnumerator();

                _current = default!;
                _nextIndex = 0;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public bool MoveNext()
            {
                if (_parent._dictionary is not null)
                {
                    bool isMore = _dictionaryEnumerator.MoveNext();

                    if (isMore)
                        _current = _dictionaryEnumerator.Current;

                    return isMore;
                }

                switch (_nextIndex)
                {
                    case 0:
                        _current = _parent._item0;
                        break;
                    case 1:
                        _current = _parent._item1;
                        break;
                    case 2:
                        _current = _parent._item2;
                        break;
                    case 3:
                        _current = _parent._item3;
                        break;
                    default:
                        return false;
                }

                _nextIndex++;
                return true;
            }
        }
    }
}
