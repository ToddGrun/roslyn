// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

internal struct ArrayWrapper<T> : IReadOnlyList<T>, IDisposable
{
    public static readonly ArrayWrapper<T> Empty = new ArrayWrapper<T>(ImmutableArray<T>.Empty);

    private ImmutableArray<T> _backingImmutableArray;
    private readonly ArrayBuilder<T>? _backingArrayBuilder;

    public ArrayWrapper(ImmutableArray<T> backingImmutableArray)
    {
        _backingImmutableArray = backingImmutableArray;
    }

    public ArrayWrapper(ArrayBuilder<T>? backingArrayBuilder)
    {
        _backingArrayBuilder = backingArrayBuilder;
    }

    public static ArrayWrapper<T> Create(T item1)
    {
        var builder = ArrayBuilder<T>.GetInstance();

        builder.Add(item1);

        return new ArrayWrapper<T>(builder);
    }

    public readonly Enumerator GetEnumerator()
    {
        return new Enumerator(in this);
    }

    public void Dispose()
    {
        _backingArrayBuilder?.Free();
    }

    public struct Enumerator
    {
        private readonly bool _backedByArrayBuilder;

        private readonly ArrayBuilder<T>.Enumerator _arrayBuilderEnumerator;
        private readonly ImmutableArray<T>.Enumerator _immutableArrayEnumerator;

        public Enumerator(in ArrayWrapper<T> symbolCollection)
        {
            if (symbolCollection._backingArrayBuilder != null)
            {
                _backedByArrayBuilder = true;
                _arrayBuilderEnumerator = symbolCollection._backingArrayBuilder.GetEnumerator();
            }
            else
            {
                _immutableArrayEnumerator = symbolCollection._backingImmutableArray.GetEnumerator();
            }
        }

        public T Current => _backedByArrayBuilder ? _arrayBuilderEnumerator.Current : _immutableArrayEnumerator.Current;
        public bool MoveNext() => _backedByArrayBuilder ? _arrayBuilderEnumerator.MoveNext() : _immutableArrayEnumerator.MoveNext();
    }

    public class EnumeratorImpl : IEnumerator<T>
    {
        private readonly Enumerator _e;

        public EnumeratorImpl(Enumerator e)
        {
            _e = e;
        }

        T IEnumerator<T>.Current => _e.Current;

        void IDisposable.Dispose()
        {
        }

        object? IEnumerator.Current => _e.Current;

        bool IEnumerator.MoveNext()
        {
            return _e.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }

    public int Count => _backingArrayBuilder != null ? _backingArrayBuilder.Count : _backingImmutableArray.Length;

    public T this[int index] => _backingArrayBuilder != null ? _backingArrayBuilder[index] : _backingImmutableArray[index];

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new EnumeratorImpl(GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new EnumeratorImpl(GetEnumerator());
    }

    // In DEBUG, swap the first and last elements of a read-only array, yielding a new read only array.
    // This helps to avoid depending on accidentally sorted arrays.
    internal void ConditionallyDeOrder()
    {
#if DEBUG
        if (_backingArrayBuilder is not null)
        {
            if (_backingArrayBuilder.Count >= 2)
            {
                T temp = _backingArrayBuilder[0];
                _backingArrayBuilder[0] = _backingArrayBuilder[^1];
                _backingArrayBuilder[^1] = temp;
            }
        }
        else
        {
            _backingImmutableArray = _backingImmutableArray.ConditionallyDeOrder();
        }
#endif
    }

    public bool IsEmpty => Count == 0;

    public ImmutableArray<T> ToImmutableArray()
    {
        if (_backingArrayBuilder is null)
            return _backingImmutableArray;

        return _backingArrayBuilder.ToImmutable();
    }

    public void Sort(IComparer<T> comparison)
    {
        if (_backingArrayBuilder is null)
            _backingImmutableArray = _backingImmutableArray.Sort(comparison);
        else
            _backingArrayBuilder.Sort(comparison);
    }

    public static ArrayWrapper<TBase> CastUp<TBase, TDerived>(ArrayWrapper<TDerived> derived)
        where TDerived : class?, TBase
    {
        if (derived._backingArrayBuilder is null)
            return new ArrayWrapper<TBase>(ImmutableArray<TBase>.CastUp(derived._backingImmutableArray));

        var builder = ArrayBuilder<TBase>.GetInstance();
        foreach (var item in derived)
        {
            builder.Add(item);
        }

        return new ArrayWrapper<TBase>(builder);
    }
}

internal static class ArrayWrapperExtensions
{
    public static bool Any<T, TArg>(this ArrayWrapper<T> array, Func<T, TArg, bool> predicate, TArg arg)
    {
        int n = array.Count;
        for (int i = 0; i < n; i++)
        {
            var a = array[i];

            if (predicate(a, arg))
            {
                return true;
            }
        }

        return false;
    }

    public static ArrayWrapper<TResult> SelectAsArrayWrapper<TResult, TSource>(this ArrayWrapper<TSource> source, Func<TSource, TResult> selector)
    {
        var builder = ArrayBuilder<TResult>.GetInstance();

        foreach (var s in source)
        {
            builder.Add(selector(s));
        }

        return new ArrayWrapper<TResult>(builder);
    }

    public static ArrayWrapper<TResult> SelectAsArrayWrapper<TResult, TArg, TSource>(this ArrayWrapper<TSource> source, Func<TSource, TArg, TResult> selector, TArg arg)
    {
        var builder = ArrayBuilder<TResult>.GetInstance();

        foreach (var s in source)
        {
            builder.Add(selector(s, arg));
        }

        return new ArrayWrapper<TResult>(builder);
    }

    public static ArrayWrapper<T> WhereAsArrayWrapper<T>(this ArrayWrapper<T> source, Func<T, bool> predicate)
    {
        var builder = ArrayBuilder<T>.GetInstance();

        foreach (var s in source)
        {
            if (predicate(s))
                builder.Add(s);
        }

        return new ArrayWrapper<T>(builder);
    }

    public static ArrayWrapper<T> WhereAsArrayWrapper<T, TArg>(this ArrayWrapper<T> source, Func<T, TArg, bool> predicate, TArg arg)
    {
        var builder = ArrayBuilder<T>.GetInstance();

        foreach (var s in source)
        {
            if (predicate(s, arg))
                builder.Add(s);
        }

        return new ArrayWrapper<T>(builder);
    }

    public static ArrayWrapper<TDest> OfType<TSource, TDest>(this ArrayWrapper<TSource> source)
    {
        var builder = ArrayBuilder<TDest>.GetInstance();
        foreach (var item in source)
        {
            if (item is TDest destItem)
                builder.Add(destItem);
        }

        return new ArrayWrapper<TDest>(builder);
    }
}
