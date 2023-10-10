// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
    {
        internal abstract Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol { get; }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
        {
            using var members = UnderlyingNamespaceOrTypeSymbol.GetMembers();
            using var publicSymbols = members.GetPublicSymbols();

            return publicSymbols.ToImmutableArray();
        }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
        {
            using var members = UnderlyingNamespaceOrTypeSymbol.GetMembers(name);
            using var publicSymbols = members.GetPublicSymbols();

            return publicSymbols.ToImmutableArray();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
        {
            using var members = UnderlyingNamespaceOrTypeSymbol.GetTypeMembers();
            using var publicSymbols = members.GetPublicSymbols();

            return publicSymbols.ToImmutableArray();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
        {
            using var members = UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name);
            using var publicSymbols = members.GetPublicSymbols();

            return publicSymbols.ToImmutableArray();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
        {
            using var members = UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name, arity);
            using var publicSymbols = members.GetPublicSymbols();

            return publicSymbols.ToImmutableArray();
        }

        bool INamespaceOrTypeSymbol.IsNamespace => UnderlyingSymbol.Kind == SymbolKind.Namespace;

        bool INamespaceOrTypeSymbol.IsType => UnderlyingSymbol.Kind != SymbolKind.Namespace;
    }
}
