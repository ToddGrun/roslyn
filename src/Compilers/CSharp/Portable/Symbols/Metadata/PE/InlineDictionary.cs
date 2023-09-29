// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal abstract class InlineDictionary<TKey, TValue> where TKey : notnull
    {
        public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value);

        public static InlineDictionary<TKey, TValue> Create(ArrayBuilder<KeyValuePair<TKey, TValue>> items, IEqualityComparer<TKey>? comparer)
        {
            var count = items.Count;
            if (count == 0)
                return InlineDictionary_Empty<TKey, TValue>.Instance;
            else if (count == 1)
                return new InlineDictionary_OneItem<TKey, TValue>(items[0], comparer);
            else if (count == 2)
                return new InlineDictionary_TwoItems<TKey, TValue>(items[0], items[1], comparer);
            else if (count == 3)
                return new InlineDictionary_ThreeItems<TKey, TValue>(items[0], items[1], items[2], comparer);
            else
                return new InlineDictionary_ManyItems<TKey, TValue>(items, count, comparer);
        }

        // TODO: shouldn't need this
        public static InlineDictionary<TKey, TValue> Create(List<KeyValuePair<TKey, TValue>> items, IEqualityComparer<TKey>? comparer)
        {
            var count = items.Count;
            if (count == 0)
                return InlineDictionary_Empty<TKey, TValue>.Instance;
            else if (count == 1)
                return new InlineDictionary_OneItem<TKey, TValue>(items[0], comparer);
            else if (count == 2)
                return new InlineDictionary_TwoItems<TKey, TValue>(items[0], items[1], comparer);
            else if (count == 3)
                return new InlineDictionary_ThreeItems<TKey, TValue>(items[0], items[1], items[2], comparer);
            else
                return new InlineDictionary_ManyItems<TKey, TValue>(items, count, comparer);
        }

        public abstract int Count { get; }

        internal static InlineDictionary<TKey, ImmutableArray<TValue>> FromArrayBuilder(ArrayBuilder<TValue> builder, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (builder.Count == 1)
            {
                var value = builder[0];
                var kvp = new KeyValuePair<TKey, ImmutableArray<TValue>>(keySelector(value), ImmutableArray.Create(value));
                var dictionary1 = new InlineDictionary_OneItem<TKey, ImmutableArray<TValue>>(kvp, comparer);
                return dictionary1;
            }

            if (builder.Count == 0)
            {
                return InlineDictionary_Empty<TKey, ImmutableArray<TValue>>.Instance;
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

            var accumulatorBuilder = ArrayBuilder<KeyValuePair<TKey, ImmutableArray<TValue>>>.GetInstance(accumulator.Count);
            try
            {
                // freeze
                foreach (var pair in accumulator)
                {
                    accumulatorBuilder.Add(new KeyValuePair<TKey, ImmutableArray<TValue>>(pair.Key, pair.Value.ToImmutableAndFree()));
                }

                var dictionary = InlineDictionary<TKey, ImmutableArray<TValue>>.Create(accumulatorBuilder, comparer);
                return dictionary;
            }
            finally
            {
                builder.Free();
            }
        }

        public abstract Enumerator GetEnumerator();

        [NonCopyable]
        public struct Enumerator
        {
            private readonly Dictionary<TKey, TValue>.Enumerator? _dictionaryEnumerator;

            private readonly int _count;

            private readonly KeyValuePair<TKey, TValue> _item1;
            private readonly KeyValuePair<TKey, TValue> _item2;
            private readonly KeyValuePair<TKey, TValue> _item3;

            private KeyValuePair<TKey, TValue> _current;
            private int _nextIndex;

            public Enumerator(KeyValuePair<TKey, TValue>? item1, KeyValuePair<TKey, TValue>? item2, KeyValuePair<TKey, TValue>? item3)
            {
                if (item1.HasValue)
                {
                    _item1 = item1.Value;
                    _count++;
                    if (item2.HasValue)
                    {
                        _item2 = item2.Value;
                        _count++;
                        if (item3.HasValue)
                        {
                            _item3 = item3.Value;
                            _count++;
                        }
                    }
                }
            }

            public Enumerator(Dictionary<TKey, TValue>.Enumerator dictionaryEnumerator)
            {
                _dictionaryEnumerator = dictionaryEnumerator;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public bool MoveNext()
            {
                if (_dictionaryEnumerator is not null)
                {
                    bool isMore = _dictionaryEnumerator.Value.MoveNext();

                    if (isMore)
                        _current = _dictionaryEnumerator.Value.Current;

                    return isMore;
                }

                if (_nextIndex >= _count)
                    return false;

                switch (_nextIndex)
                {
                    case 0:
                        _current = _item1;
                        break;
                    case 1:
                        _current = _item2;
                        break;
                    case 2:
                        _current = _item3;
                        break;
                }

                _nextIndex++;
                return true;
            }
        }
    }

    internal sealed class InlineDictionary_Empty<TKey, TValue>
        : InlineDictionary<TKey, TValue> where TKey : notnull
    {
        public static InlineDictionary_Empty<TKey, TValue> Instance = new();

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            value = default;
            return false;
        }

        public override int Count => 0;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }
    }

    internal sealed class InlineDictionary_OneItem<TKey, TValue>
        : InlineDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly KeyValuePair<TKey, TValue> _item;

        public InlineDictionary_OneItem(KeyValuePair<TKey, TValue> item, IEqualityComparer<TKey>? comparer)
        {
            _item = item;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            if (_comparer.Equals(key, _item.Key))
            {
                value = _item.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override int Count => 1;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator(_item, null, null);
        }
    }

    internal sealed class InlineDictionary_TwoItems<TKey, TValue>
        : InlineDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly KeyValuePair<TKey, TValue> _item1;
        private readonly KeyValuePair<TKey, TValue> _item2;

        public InlineDictionary_TwoItems(KeyValuePair<TKey, TValue> item1, KeyValuePair<TKey, TValue> item2, IEqualityComparer<TKey>? comparer)
        {
            _item1 = item1;
            _item2 = item2;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            if (_comparer.Equals(key, _item1.Key))
            {
                value = _item1.Value;
                return true;
            }

            if (_comparer.Equals(key, _item2.Key))
            {
                value = _item2.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override int Count => 2;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator(_item1, _item2, null);
        }
    }

    internal sealed class InlineDictionary_ThreeItems<TKey, TValue>
        : InlineDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly KeyValuePair<TKey, TValue> _item1;
        private readonly KeyValuePair<TKey, TValue> _item2;
        private readonly KeyValuePair<TKey, TValue> _item3;

        public InlineDictionary_ThreeItems(KeyValuePair<TKey, TValue> item1, KeyValuePair<TKey, TValue> item2, KeyValuePair<TKey, TValue> item3, IEqualityComparer<TKey>? comparer)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            if (_comparer.Equals(key, _item1.Key))
            {
                value = _item1.Value;
                return true;
            }

            if (_comparer.Equals(key, _item2.Key))
            {
                value = _item2.Value;
                return true;
            }

            if (_comparer.Equals(key, _item3.Key))
            {
                value = _item3.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override int Count => 3;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator(_item1, _item2, _item3);
        }
    }

    internal class InlineDictionary_ManyItems<TKey, TValue>
        : InlineDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public InlineDictionary_ManyItems(IEnumerable<KeyValuePair<TKey, TValue>> items, int count, IEqualityComparer<TKey>? comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(count, comparer);

            foreach (var kvp in items)
            {
                _dictionary.Add(kvp.Key, kvp.Value);
            }
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public override int Count => _dictionary.Count;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary.GetEnumerator());
        }
    }
}
