﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class NameGenerator
    {
        /// <summary>
        /// Transforms baseName into a name that does not conflict with any name in 'reservedNames'
        /// </summary>
        public static string EnsureUniqueness(
            string baseName,
            IEnumerable<string> reservedNames,
            bool isCaseSensitive = true)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var pooledNameSet);
            HashSet<string> nameSet = pooledNameSet;

            if (!isCaseSensitive)
            {
                // The pooled collections don't allow non-default comparers (and they can't be set on
                //   dictionary/hashset after creation). As this scenario is infrequent, we'll just
                //   create allocate new objects for these. The pooled object will still be freed
                //   via the PooledDisposer.
                nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var index = 1;
            var result = baseName;
            while (nameSet.Contains(result))
            {
                result = baseName + index;
            }

            return result;
        }

        public static ImmutableArray<string> EnsureUniqueness(
            ImmutableArray<string> names,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            using var isFixedDisposer = ArrayBuilder<bool>.GetInstance(names.Length, fillWithValue: false, out var isFixed);

            var result = ArrayBuilder<string>.GetInstance(names.Length);
            result.AddRange(names);
            EnsureUniquenessInPlace(result, isFixed, canUse, isCaseSensitive);
            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Ensures that any 'names' is unique and does not collide with any other name.  Names that
        /// are marked as IsFixed can not be touched.  This does mean that if there are two names
        /// that are the same, and both are fixed that you will end up with non-unique names at the
        /// end.
        /// </summary>
        public static ImmutableArray<string> EnsureUniqueness(
            ImmutableArray<string> names,
            ImmutableArray<bool> isFixed,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            using var _1 = ArrayBuilder<bool>.GetInstance(names.Length, out var isFixedBuilder);
            using var _2 = ArrayBuilder<string>.GetInstance(names.Length, out var result);

            isFixedBuilder.AddRange(isFixed);
            result.AddRange(names);

            EnsureUniquenessInPlace(result, isFixedBuilder, canUse, isCaseSensitive);

            return result.ToImmutableAndClear();
        }

        /// <summary>
        /// Updates the names in <paramref name="names"/> to be unique.  A name at a particular
        /// index <c>i</c> will not be touched if <c>isFixed[i]</c> is <see langword="true"/>. All
        /// other names will not collide with any other in <paramref name="names"/> and will all
        /// return <see langword="true"/> for <c>canUse(name)</c>.
        /// </summary>
        public static void EnsureUniquenessInPlace(
            ArrayBuilder<string> names,
            ArrayBuilder<bool> isFixed,
            Func<string, bool>? canUse = null,
            bool isCaseSensitive = true)
        {
            canUse ??= Functions<string>.True;

            using var _1 = PooledDictionary<string, bool>.GetInstance(out var pooledCollisionMap);
            using var _2 = PooledHashSet<string>.GetInstance(out var pooledUsedNames);

            Dictionary<string, bool> collisionMap = pooledCollisionMap;
            HashSet<string> usedNames = pooledUsedNames;

            if (!isCaseSensitive)
            {
                // The pooled collections don't allow non-default comparers (and they can't be set on
                //   dictionary/hashset after creation). As this scenario is infrequent, we'll just
                //   create allocate new objects for these. The pooled objects will still be freed
                //   via the PooledDisposers.
                collisionMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Initial pass through names to determine which names are in collision
            foreach (var name in names)
            {
                if (!collisionMap.ContainsKey(name))
                {
                    collisionMap.Add(name, false);
                }
                else
                {
                    collisionMap.Add(name, true);
                    usedNames.Add(name);
                }
            }

            // Update any name that is in collision (and not fixed) to have
            //   a new name that hasn't yet been placed in usedNames.
            for (var i = 0; i < names.Count; i++)
            {
                if (isFixed[i])
                    continue;

                var name = names[i];
                if (!collisionMap[name] && canUse(name))
                    continue;

                var index = 1;
                var updatedName = name + index;
                while (usedNames.Contains(updatedName) || !canUse(updatedName))
                    updatedName = name + index;

                usedNames.Add(updatedName);
                names[i] = updatedName;
            }
        }

        public static string GenerateUniqueName(string baseName, Func<string, bool> canUse)
            => GenerateUniqueName(baseName, string.Empty, canUse);

        public static string GenerateUniqueName(string baseName, ISet<string> names, StringComparer comparer)
            => GenerateUniqueName(baseName, x => !names.Contains(x, comparer));

        public static string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
        {
            if (!string.IsNullOrEmpty(extension) && extension[0] != '.')
            {
                extension = "." + extension;
            }

            var name = baseName + extension;
            var index = 1;

            // Check for collisions
            while (!canUse(name))
            {
                name = baseName + index + extension;
                index++;
            }

            return name;
        }

        public static string GenerateUniqueName(IEnumerable<string> baseNames, Func<string, bool> canUse)
        {
            int? index = null;

            while (true)
            {
                foreach (var name in baseNames)
                {
                    var modifiedName = name + index;

                    if (canUse(modifiedName))
                        return modifiedName;
                }

                index = index is null ? 1 : index + 1;
            }
        }
    }
}
