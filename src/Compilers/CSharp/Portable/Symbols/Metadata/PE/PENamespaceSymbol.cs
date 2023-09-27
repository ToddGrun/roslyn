// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal struct FrugalDictionary<TKey, TValue> where TKey : notnull
    {
        private const int InlineCapacity = 4;

        private readonly IEqualityComparer<TKey> _comparer;

        private KeyValuePair<TKey, TValue> _item0;
        private KeyValuePair<TKey, TValue> _item1;
        private KeyValuePair<TKey, TValue> _item2;
        private KeyValuePair<TKey, TValue> _item3;

        private Dictionary<TKey, TValue>? _dictionary;

        private int _count;

        public FrugalDictionary(IEqualityComparer<TKey>? comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public static FrugalDictionary<TKey, TValue> Empty => default;

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

#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<KeyValuePair<TKey, TValue>>())
#endif
            {
                _item0 = default!;
                _item1 = default!;
                _item2 = default!;
                _item3 = default!;
            }

            _count = 0;
            _dictionary = dictionary;
        }

        public readonly int Count => _dictionary?.Count ?? _count;

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

            return false;
        }

        public void AddValuesToArrayBuilder(ArrayBuilder<TValue> values)
        {
            if (_dictionary is not null)
            {
                foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
                {
                    values.Add(kvp.Value);
                }
                return;
            }

            if (_count == 0)
                return;

            values.Add(_item0.Value);

            if (_count == 1)
                return;

            values.Add(_item1.Value);

            if (_count == 2)
                return;

            values.Add(_item2.Value);

            if (_count == 3)
                return;

            values.Add(_item3.Value);
        }
    }

    /// <summary>
    /// The base class to represent a namespace imported from a PE/module. Namespaces that differ
    /// only by casing in name are not merged.
    /// </summary>
    internal abstract class PENamespaceSymbol
        : NamespaceSymbol
    {
        /// <summary>
        /// A map of namespaces immediately contained within this namespace 
        /// mapped by their name (case-sensitively).
        /// </summary>
        protected Dictionary<ReadOnlyMemory<char>, PENestedNamespaceSymbol> lazyNamespaces;

        protected FrugalDictionary<ReadOnlyMemory<char>, PENestedNamespaceSymbol>? lazyNamespaces2;

        /// <summary>
        /// A map of types immediately contained within this namespace 
        /// grouped by their name (case-sensitively).
        /// </summary>
        protected Dictionary<ReadOnlyMemory<char>, ImmutableArray<PENamedTypeSymbol>> lazyTypes;

        /// <summary>
        /// A map of NoPia local types immediately contained in this assembly.
        /// Maps type name (non-qualified) to the row id. Note, for VB we should use
        /// full name.
        /// </summary>
        private Dictionary<string, TypeDefinitionHandle> _lazyNoPiaLocalTypes;

        /// <summary>
        /// All type members in a flat array
        /// </summary>
        private ImmutableArray<PENamedTypeSymbol> _lazyFlattenedTypes;

        internal sealed override NamespaceExtent Extent
        {
            get
            {
                return new NamespaceExtent(this.ContainingPEModule);
            }
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Provide " + nameof(ArrayBuilder<Symbol>) + " capacity to reduce number of allocations.",
            AllowGenericEnumeration = false)]
        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersLoaded();

            var memberTypes = GetMemberTypesPrivate();

            if (lazyNamespaces2.Value.Count == 0)
                return StaticCast<Symbol>.From(memberTypes);

            var builder = ArrayBuilder<Symbol>.GetInstance(memberTypes.Length + lazyNamespaces2.Value.Count);

            builder.AddRange(memberTypes);
            lazyNamespaces2.Value.AddValuesToArrayBuilder(builder);
            //foreach (var pair in lazyNamespaces2.Value)
            //{
            //    builder.Add(pair.Value);
            //}

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<NamedTypeSymbol> GetMemberTypesPrivate()
        {
            //assume that EnsureAllMembersLoaded() has initialize lazyTypes
            if (_lazyFlattenedTypes.IsDefault)
            {
                var flattened = lazyTypes.Flatten();
                ImmutableInterlocked.InterlockedExchange(ref _lazyFlattenedTypes, flattened);
            }

            return StaticCast<NamedTypeSymbol>.From(_lazyFlattenedTypes);
        }

        public sealed override ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name)
        {
            EnsureAllMembersLoaded();

            PENestedNamespaceSymbol ns = null;
            ImmutableArray<PENamedTypeSymbol> t;

            if (lazyNamespaces2.Value.TryGetValue(name, out ns))
            {
                if (lazyTypes.TryGetValue(name, out t))
                {
                    // TODO - Eliminate the copy by storing all members and type members instead of non-type and type members?
                    return StaticCast<Symbol>.From(t).Add(ns);
                }
                else
                {
                    return ImmutableArray.Create<Symbol>(ns);
                }
            }
            else if (lazyTypes.TryGetValue(name, out t))
            {
                return StaticCast<Symbol>.From(t);
            }

            return ImmutableArray<Symbol>.Empty;
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            EnsureAllMembersLoaded();

            return GetMemberTypesPrivate();
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            EnsureAllMembersLoaded();

            ImmutableArray<PENamedTypeSymbol> t;

            return lazyTypes.TryGetValue(name, out t)
                ? StaticCast<NamedTypeSymbol>.From(t)
                : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray((type, arity) => type.Arity == arity, arity);
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        /// <summary>
        /// Returns PEModuleSymbol containing the namespace.
        /// </summary>
        /// <returns>PEModuleSymbol containing the namespace.</returns>
        internal abstract PEModuleSymbol ContainingPEModule { get; }

        [MemberNotNull(nameof(lazyNamespaces2))]
        protected abstract void EnsureAllMembersLoaded();

        /// <summary>
        /// Initializes namespaces and types maps with information about 
        /// namespaces and types immediately contained within this namespace.
        /// </summary>
        /// <param name="typesByNS">
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids must be grouped by the 
        /// fully-qualified namespace name case-sensitively. There could be multiple groups 
        /// for each fully-qualified namespace name. The groups must be sorted by
        /// their key in case-sensitive manner. Empty string must be used as namespace name for types 
        /// immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        /// must be in several first IGroupings.
        /// </param>
        protected void LoadAllMembers(IEnumerable<IGrouping<string, TypeDefinitionHandle>> typesByNS)
        {
            Debug.Assert(typesByNS != null);

            // A sequence of groups of TypeDef row ids for types immediately contained within this namespace.
            IEnumerable<IGrouping<string, TypeDefinitionHandle>> nestedTypes = null;

            // A sequence with information about namespaces immediately contained within this namespace.
            // For each pair:
            //    Key - contains simple name of a child namespace.
            //    Value - contains a sequence similar to the one passed to this function, but
            //            calculated for the child namespace. 
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>> nestedNamespaces = null;
            bool isGlobalNamespace = this.IsGlobalNamespace;

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                isGlobalNamespace,
                isGlobalNamespace ? 0 : GetQualifiedNameLength(),
                typesByNS,
                StringComparer.Ordinal,
                out nestedTypes, out nestedNamespaces);

            LazyInitializeNamespaces(nestedNamespaces);

            LazyInitializeTypes(nestedTypes);
        }

        private int GetQualifiedNameLength()
        {
            int length = this.Name.Length;

            var parent = ContainingNamespace;
            while (parent?.IsGlobalNamespace == false)
            {
                // add name of the parent + "."
                length += parent.Name.Length + 1;
                parent = parent.ContainingNamespace;
            }

            return length;
        }

        /// <summary>
        /// Create symbols for nested namespaces and initialize namespaces map.
        /// </summary>
        private void LazyInitializeNamespaces(
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>> childNamespaces)
        {
            if (!this.lazyNamespaces2.HasValue)
            {
                var namespaces = new FrugalDictionary<ReadOnlyMemory<char>, PENestedNamespaceSymbol>(ReadOnlyMemoryOfCharComparer.Instance);

                foreach (var child in childNamespaces)
                {
                    var c = new PENestedNamespaceSymbol(child.Key, this, child.Value);
                    namespaces.Add(c.Name.AsMemory(), c);
                }

                Interlocked.CompareExchange(ref this.lazyNamespaces2, namespaces, null);
            }
        }

        /// <summary>
        /// Create symbols for nested types and initialize types map.
        /// </summary>
        private void LazyInitializeTypes(IEnumerable<IGrouping<string, TypeDefinitionHandle>> typeGroups)
        {
            if (this.lazyTypes == null)
            {
                var moduleSymbol = ContainingPEModule;

                var children = ArrayBuilder<PENamedTypeSymbol>.GetInstance();
                var skipCheckForPiaType = !moduleSymbol.Module.ContainsNoPiaLocalTypes();
                Dictionary<string, TypeDefinitionHandle> noPiaLocalTypes = null;

                foreach (var g in typeGroups)
                {
                    foreach (var t in g)
                    {
                        if (skipCheckForPiaType || !moduleSymbol.Module.IsNoPiaLocalType(t))
                        {
                            children.Add(PENamedTypeSymbol.Create(moduleSymbol, this, t, g.Key));
                        }
                        else
                        {
                            try
                            {
                                string typeDefName = moduleSymbol.Module.GetTypeDefNameOrThrow(t);

                                if (noPiaLocalTypes == null)
                                {
                                    noPiaLocalTypes = new Dictionary<string, TypeDefinitionHandle>(StringOrdinalComparer.Instance);
                                }

                                noPiaLocalTypes[typeDefName] = t;
                            }
                            catch (BadImageFormatException)
                            { }
                        }
                    }
                }

                var typesDict = children.ToDictionary(c => c.Name.AsMemory(), ReadOnlyMemoryOfCharComparer.Instance);
                children.Free();

                if (noPiaLocalTypes != null)
                {
                    Interlocked.CompareExchange(ref _lazyNoPiaLocalTypes, noPiaLocalTypes, null);
                }

                var original = Interlocked.CompareExchange(ref this.lazyTypes, typesDict, null);

                // Build cache of TypeDef Tokens
                // Potentially this can be done in the background.
                if (original == null)
                {
                    moduleSymbol.OnNewTypeDeclarationsLoaded(typesDict);
                }
            }
        }

#nullable enable

        internal NamedTypeSymbol? UnifyIfNoPiaLocalType(ref MetadataTypeName emittedTypeName)
        {
            EnsureAllMembersLoaded();
            TypeDefinitionHandle typeDef;

            // See if this is a NoPia local type, which we should unify.
            // Note, VB should use FullName.
            if (_lazyNoPiaLocalTypes != null && _lazyNoPiaLocalTypes.TryGetValue(emittedTypeName.TypeName, out typeDef))
            {
                var result = (NamedTypeSymbol)new MetadataDecoder(ContainingPEModule).GetTypeOfToken(typeDef, out bool isNoPiaLocalType);
                Debug.Assert(isNoPiaLocalType);
                Debug.Assert(result is not null);
                return result;
            }

            return null;
        }
    }
}
