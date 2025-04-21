// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal static class SymbolContainment
{
    public static async Task<IEnumerable<SyntaxNode>> GetContainedSyntaxNodesAsync(Document document, CancellationToken cancellationToken)
    {
        var progressionLanguageService = document.GetLanguageService<IProgressionLanguageService>();
        if (progressionLanguageService == null)
            return [];

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return [];

        return progressionLanguageService.GetTopLevelNodesFromDocument(root, cancellationToken);
    }

    public static async Task<ImmutableArray<ISymbol>> GetContainedSymbolsAsync(Document document, CancellationToken cancellationToken)
    {
        var syntaxNodes = await GetContainedSyntaxNodesAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

        foreach (var syntaxNode in syntaxNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol != null &&
                !string.IsNullOrEmpty(symbol.Name) &&
                IsTopLevelSymbol(symbol))
            {
                symbols.Add(symbol);
            }
        }

        return symbols.ToImmutableAndClear();
    }

    private static bool IsTopLevelSymbol(ISymbol symbol)
    {
        return symbol.Kind switch
        {
            SymbolKind.NamedType or SymbolKind.Method or SymbolKind.Field or SymbolKind.Property or SymbolKind.Event => true,
            _ => false,
        };
    }

    public static ImmutableArray<ISymbol> GetContainedSymbols(ISymbol symbol)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);

        if (symbol is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member.IsImplicitlyDeclared)
                    continue;

                if (member is IMethodSymbol method && method.AssociatedSymbol != null)
                    continue;

                if (!string.IsNullOrEmpty(member.Name))
                    builder.Add(member);
            }
        }

        return builder.ToImmutableAndClear();
    }
}
