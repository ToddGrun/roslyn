// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Collections;

internal static class ArrayWrapperExtensions
{
    public static bool All<T>(this ArrayWrapper<T> array, Func<T, bool> predicate)
    {
        int n = array.Count;
        for (int i = 0; i < n; i++)
        {
            var a = array[i];

            if (!predicate(a))
            {
                return false;
            }
        }

        return true;
    }

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

    public static TValue? FirstOrDefault<TValue>(this ArrayWrapper<TValue> array)
    {
        if (array.Count > 0)
            return array[0];

        return default;
    }

    public static TValue? FirstOrDefault<TValue>(this ArrayWrapper<TValue> array, Func<TValue, bool> predicate)
    {
        foreach (var val in array)
        {
            if (predicate(val))
            {
                return val;
            }
        }

        return default;
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
