// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="MissingNamespaceSymbol"/> is a special kind of <see cref="NamespaceSymbol"/> that represents
    /// a namespace that couldn't be found.
    /// </summary>
    internal class MissingNamespaceSymbol : NamespaceSymbol
    {
        private readonly string _name;
        private readonly Symbol _containingSymbol;

        public MissingNamespaceSymbol(MissingModuleSymbol containingModule)
        {
            Debug.Assert((object)containingModule != null);

            _containingSymbol = containingModule;
            _name = string.Empty;
        }

        public MissingNamespaceSymbol(NamespaceSymbol containingNamespace, string name)
        {
            Debug.Assert((object)containingNamespace != null);
            Debug.Assert(name != null);

            _containingSymbol = containingNamespace;
            _name = name;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _containingSymbol.ContainingAssembly;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                if (_containingSymbol.Kind == SymbolKind.NetModule)
                {
                    return new NamespaceExtent((ModuleSymbol)_containingSymbol);
                }

                return ((NamespaceSymbol)_containingSymbol).Extent;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_containingSymbol.GetHashCode(), _name.GetHashCode());
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            MissingNamespaceSymbol other = obj as MissingNamespaceSymbol;

            return (object)other != null && _name.Equals(other._name) && _containingSymbol.Equals(other._containingSymbol, compareKind);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ArrayWrapper<NamedTypeSymbol> GetTypeMembers()
        {
            return ArrayWrapper<NamedTypeSymbol>.Empty;
        }

        public override ArrayWrapper<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return ArrayWrapper<NamedTypeSymbol>.Empty;
        }

        public override ArrayWrapper<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return ArrayWrapper<NamedTypeSymbol>.Empty;
        }

        public override ArrayWrapper<Symbol> GetMembers()
        {
            return ArrayWrapper<Symbol>.Empty;
        }

        public override ArrayWrapper<Symbol> GetMembers(ReadOnlyMemory<char> name)
        {
            return ArrayWrapper<Symbol>.Empty;
        }
    }
}
