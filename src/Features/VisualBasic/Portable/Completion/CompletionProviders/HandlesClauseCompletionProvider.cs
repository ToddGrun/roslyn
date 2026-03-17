// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(HandlesClauseCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(ImplementsClauseCompletionProvider))]
[Shared]
internal partial class HandlesClauseCompletionProvider : AbstractSymbolCompletionProvider<VisualBasicSyntaxContext>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public HandlesClauseCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    protected override async Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext completionContext,
        VisualBasicSyntaxContext syntaxContext,
        int position,
        CompletionOptions options,
        CancellationToken cancellationToken)
    {
        var symbols = await GetSymbolsAsync(syntaxContext, position, cancellationToken).ConfigureAwait(false);
        return symbols.SelectAsArray(s => new SymbolAndSelectionInfo(s, Preselect: false));
    }

    private static Task<ImmutableArray<ISymbol>> GetSymbolsAsync(VisualBasicSyntaxContext context, int position, CancellationToken cancellationToken)
    {
        if (context.SyntaxTree.IsInNonUserCode(position, cancellationToken) ||
            context.SyntaxTree.IsInSkippedText(position, cancellationToken))
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        if (context.TargetToken.Kind() == SyntaxKind.None)
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        // Handles or a comma
        if (context.TargetToken.IsChildToken<HandlesClauseSyntax>(static hc => hc.HandlesKeyword) ||
            context.TargetToken.IsChildSeparatorToken<HandlesClauseSyntax>(static hc => hc.Events))
        {
            return Task.FromResult(GetTopLevelIdentifiers(context, cancellationToken));
        }

        // Handles x. or , x.
        if (context.TargetToken.IsChildToken<HandlesClauseItemSyntax>(static hc => hc.DotToken))
        {
            return Task.FromResult(LookUpEvents(context, context.TargetToken, cancellationToken));
        }

        return SpecializedTasks.EmptyImmutableArray<ISymbol>();
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerChars;

    private static ImmutableArray<ISymbol> GetTopLevelIdentifiers(
        VisualBasicSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken);
        var containingType = containingSymbol as ITypeSymbol;
        if (containingType == null)
        {
            // We got the containing method as our enclosing type.
            containingType = containingSymbol.ContainingType;
        }

        if (containingType == null)
        {
            // We've somehow failed to find a containing type.
            return ImmutableArray<ISymbol>.Empty;
        }

        // Instance or shared variables declared WithEvents
        var symbols = context.SemanticModel.LookupSymbols(context.Position, (INamespaceOrTypeSymbol)containingType, includeReducedExtensionMethods: true);
        return symbols.WhereAsArray(s => IsWithEvents(s));
    }

    private static ImmutableArray<ISymbol> LookUpEvents(
        VisualBasicSyntaxContext context,
        SyntaxToken token,
        CancellationToken cancellationToken)
    {
        // We came up on a dot, so the previous token will tell us in which object we should find events.
        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken);
        var containingType = containingSymbol as ITypeSymbol;
        if (containingType == null)
        {
            // We got the containing method as our enclosing type.
            containingType = containingSymbol.ContainingType;
        }

        if (containingType == null)
        {
            // We've somehow failed to find a containing type.
            return ImmutableArray<ISymbol>.Empty;
        }

        var result = ImmutableArray<IEventSymbol>.Empty;

        var previousToken = token.GetPreviousToken();
        switch (previousToken.Kind())
        {
            case SyntaxKind.MeKeyword:
            case SyntaxKind.MyClassKeyword:
                result = context.SemanticModel.LookupSymbols(context.Position, containingType)
                    .OfType<IEventSymbol>()
                    .ToImmutableArray();
                break;
            case SyntaxKind.MyBaseKeyword:
                result = context.SemanticModel.LookupSymbols(context.Position, containingType.BaseType)
                    .OfType<IEventSymbol>()
                    .ToImmutableArray();
                break;
            case SyntaxKind.IdentifierToken:
                // We must be looking at a WithEvents property.
                var symbolInfo = context.SemanticModel.GetSymbolInfo(previousToken, cancellationToken);
                if (symbolInfo.Symbol != null)
                {
                    var type = (symbolInfo.Symbol as IPropertySymbol)?.Type;
                    if (type != null)
                    {
                        result = context.SemanticModel.LookupSymbols(token.SpanStart, type)
                            .OfType<IEventSymbol>()
                            .ToImmutableArray();
                    }
                }
                break;
        }

        return ImmutableArray<ISymbol>.CastUp(result);
    }

    private static bool IsWithEvents(ISymbol s)
    {
        var property = s as IPropertySymbol;
        if (property != null)
        {
            return property.IsWithEvents;
        }

        return false;
    }
}
