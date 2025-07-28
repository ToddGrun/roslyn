// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.PooledObjects;

/// <summary>
/// Represents a hybrid dictionary that uses an array for small collections and switches to a dictionary for larger collections.
/// This class differs from the framework's HybridDictionary by:
/// 1) Defined as a struct to avoid heap allocations.
/// 2) For small collections, uses an array instead of a ListDictionary (ListDictionary is implemented as a linked list).
/// 3) For larger collections, uses a Dictionary instead of a Hashtable.
/// 4) Exposes a struct enumerator to avoid heap allocations during enumeration, and utilizes the struct enumerator from Dictionary.
/// 5) Utilizes a pool to allow array reuse when becoming a "large" collection.
/// </summary>
internal class PooledHybridDictionary<TKey, TValue>
#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
    : IPooled
#endif
    where TKey : notnull
{
    /// <summary>
    ///  The number of items that can be stored in the array before switching over to the dictionary.
    ///  Depending on hashing and equality costs and the number of items in the eventual collection,
    ///  the optimal value for this would vary, but 5 was found to be a relatively good value for most
    ///  combinations.
    /// </summary>
    private const int MaxArrayCapacity = 5;

    private static readonly ObjectPool<PooledHybridDictionary<TKey, TValue?>> s_poolInstance = CreatePool();

    private readonly ObjectPool<PooledHybridDictionary<TKey, TValue?>> _pool;

    private readonly (int Hash, TKey Key, TValue? Value)[] _array;
    private int _arrayCount = 0;
    private bool _inArrayMode;

    private readonly Dictionary<TKey, TValue?> _dictionary;
    private readonly IEqualityComparer<TKey> _comparer;

    private PooledHybridDictionary(ObjectPool<PooledHybridDictionary<TKey, TValue?>> pool, Dictionary<TKey, TValue?> dictionary)
    {
        _array = new (int, TKey, TValue?)[MaxArrayCapacity];
        _arrayCount = 0;
        _dictionary = dictionary;
        _comparer = dictionary.Comparer ?? EqualityComparer<TKey>.Default;
        _pool = pool;
        _inArrayMode = true;
    }

#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
    public static PooledDisposer<PooledHybridDictionary<TKey, TValue?>> GetInstance(out PooledHybridDictionary<TKey, TValue?> instance)
    {
        instance = GetInstance();
        return new PooledDisposer<PooledHybridDictionary<TKey, TValue?>>(instance);
    }

    // Nothing special to do here.
    void IPooled.Free(bool discardLargeInstance)
        => this.Free();
#endif

    public static PooledHybridDictionary<TKey, TValue?> GetInstance()
        => GetInstance(s_poolInstance);

    public static PooledHybridDictionary<TKey, TValue?> GetInstance(ObjectPool<PooledHybridDictionary<TKey, TValue?>> pool)
    {
        var instance = pool.Allocate();
        Debug.Assert(instance.Count == 0);
        return instance;
    }

    public static ObjectPool<PooledHybridDictionary<TKey, TValue?>> CreatePool(IEqualityComparer<TKey>? keyComparer = null)
    {
        ObjectPool<PooledHybridDictionary<TKey, TValue?>>? pool = null;
        pool = new ObjectPool<PooledHybridDictionary<TKey, TValue?>>(() => new PooledHybridDictionary<TKey, TValue?>(pool!, new Dictionary<TKey, TValue?>(MaxArrayCapacity + 1, keyComparer)), 16);
        return pool;
    }

    public void Free()
    {
        if (_inArrayMode)
        {
            if (_arrayCount > 0)
            {
                Array.Clear(_array, 0, _arrayCount);
                _arrayCount = 0;
            }
        }
        else
        {
            _inArrayMode = true;
            _dictionary.Clear();
        }

        _pool.Free(this);
    }

    public int Count
        => _inArrayMode ? _arrayCount : _dictionary.Count;

    public Enumerator GetEnumerator()
        => new(this);

    public TValue? this[TKey key]
    {
        get
        {
            if (_inArrayMode)
            {
                if (_arrayCount > 0)
                {
                    var hash = _comparer.GetHashCode(key);
                    for (var i = 0; i < _arrayCount; i++)
                    {
                        var element = _array[i];
                        if (hash == element.Hash && _comparer.Equals(element.Key, key))
                        {
                            return element.Value;
                        }
                    }
                }

                throw new KeyNotFoundException();
            }
            else
            {
                return _dictionary[key];
            }
        }

        set
        {
            if (_inArrayMode)
            {
                var hash = _comparer.GetHashCode(key);
                for (var i = 0; i < _arrayCount; i++)
                {
                    var element = _array[i];
                    if (hash == element.Hash && _comparer.Equals(element.Key, key))
                    {
                        _array[i] = (hash, key, value);
                        return;
                    }
                }

                AppendToArray(key, value, hash);
            }
            else
            {
                _dictionary[key] = value;
            }
        }
    }

    private void AppendToArray(TKey key, TValue? value, int hash)
    {
        if (_array.Length == _arrayCount)
        {
            Debug.Assert(_arrayCount == MaxArrayCapacity);

            MoveArrayItemsToDictionary();
            _dictionary[key] = value;
        }
        else
        {
            _array[_arrayCount++] = (hash, key, value);
        }
    }

    private void MoveArrayItemsToDictionary()
    {
        // Add the inline items and clear their field values.
        foreach (var element in _array)
        {
            _dictionary[element.Key] = element.Value;
        }

        Array.Clear(_array, 0, _arrayCount);
        _arrayCount = 0;
        _inArrayMode = false;
    }

    public bool ContainsKey(TKey key)
    {
        if (_inArrayMode)
        {
            if (_arrayCount > 0)
            {
                var hash = _comparer.GetHashCode(key);

                return ArrayContainsKey(key, hash);
            }

            return false;
        }
        else
        {
            return _dictionary.ContainsKey(key);
        }
    }

    private bool ArrayContainsKey(TKey key, int hash)
    {
        for (var i = 0; i < _arrayCount; i++)
        {
            var element = _array[i];
            if (hash == element.Hash && _comparer.Equals(element.Key, key))
            {
                return true;
            }
        }

        return false;
    }

    public void Add(TKey key, TValue? value)
    {
        if (_inArrayMode)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key), "key cannot be null");
            }

            var hash = _comparer.GetHashCode(key);
            if (ArrayContainsKey(key, hash))
            {
                throw new ArgumentException("added duplicate key", nameof(key));
            }

            AppendToArray(key, value, hash);
        }
        else
        {
            _dictionary.Add(key, value);
        }
    }

    public void Remove(TKey key)
    {
        if (_inArrayMode)
        {
            if (_arrayCount > 0)
            {
                var hash = _comparer.GetHashCode(key);

                for (var i = 0; i < _arrayCount; i++)
                {
                    var element = _array[i];
                    if (hash == element.Hash && _comparer.Equals(element.Key, key))
                    {
                        var lastFilledArrayIndex = _arrayCount - 1;
                        if (i < lastFilledArrayIndex)
                        {
                            // Move the last item to the current position as order doesn't matter.
                            _array[i] = _array[lastFilledArrayIndex];
                            _array[lastFilledArrayIndex] = default;
                        }

                        _arrayCount--;
                        break;
                    }
                }
            }
        }
        else
        {
            _ = _dictionary.Remove(key);
        }
    }

    public struct Enumerator(PooledHybridDictionary<TKey, TValue?> dictionary) : IDisposable
    {
        private readonly PooledHybridDictionary<TKey, TValue?> _dictionary = dictionary;
        private Dictionary<TKey, TValue?>.Enumerator _dictionaryEnumerator = dictionary._inArrayMode ? default : dictionary._dictionary.GetEnumerator();
        private int _arrayIndex = -1;

        public KeyValuePair<TKey, TValue?> Current { get; private set; }

        public void Dispose()
        {
            if (!_dictionary._inArrayMode)
                _dictionaryEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            if (_dictionary._inArrayMode)
            {
                if (++_arrayIndex < _dictionary._arrayCount)
                {
                    var item = _dictionary._array[_arrayIndex];
                    Current = new KeyValuePair<TKey, TValue?>(item.Key, item.Value);
                    return true;
                }
            }
            else
            {
                if (_dictionaryEnumerator.MoveNext())
                {
                    Current = _dictionaryEnumerator.Current;
                    return true;
                }
            }

            return false;
        }
    }
}

