// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Symbols;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(ImplementsClauseCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(VisualBasicSuggestionModeCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ImplementsClauseCompletionProvider() : AbstractSymbolCompletionProvider<VisualBasicSyntaxContext>
{
    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerChars;

    protected override bool IsExclusive()
    {
        return true;
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
        return symbols.SelectAsArray(s => new SymbolAndSelectionInfo(Symbol: s, Preselect: false));
    }

    private Task<ImmutableArray<ISymbol>> GetSymbolsAsync(
        VisualBasicSyntaxContext context, int position, CancellationToken cancellationToken)
    {
        if (context.TargetToken.Kind() == SyntaxKind.None)
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        if (context.SyntaxTree.IsInNonUserCode(position, cancellationToken) ||
            context.SyntaxTree.IsInSkippedText(position, cancellationToken))
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        // We only care about Methods, Properties, and Events
        SyntaxKind memberKindKeyword = default;
        var methodDeclaration = context.TargetToken.GetAncestor<MethodStatementSyntax>();
        if (methodDeclaration != null)
        {
            memberKindKeyword = methodDeclaration.DeclarationKeyword.Kind();
        }

        var propertyDeclaration = context.TargetToken.GetAncestor<PropertyStatementSyntax>();
        if (propertyDeclaration != null)
        {
            memberKindKeyword = propertyDeclaration.DeclarationKeyword.Kind();
        }

        var eventDeclaration = context.TargetToken.GetAncestor<EventStatementSyntax>();
        if (eventDeclaration != null)
        {
            memberKindKeyword = eventDeclaration.DeclarationKeyword.Kind();
        }

        // We couldn't find a declaration. Bail.
        if (memberKindKeyword == default)
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        var result = ImmutableArray<ISymbol>.Empty;

        // Valid positions: Immediately after 'Implements, after ., or after a ,
        if (context.TargetToken.Kind() == SyntaxKind.ImplementsKeyword && context.TargetToken.Parent.IsKind(SyntaxKind.ImplementsClause))
        {
            result = GetInterfacesAndContainers(position, context.TargetToken.Parent, context.SemanticModel, memberKindKeyword, cancellationToken);
        }

        if (context.TargetToken.Kind() == SyntaxKind.CommaToken && context.TargetToken.Parent.IsKind(SyntaxKind.ImplementsClause))
        {
            result = GetInterfacesAndContainers(position, context.TargetToken.Parent, context.SemanticModel, memberKindKeyword, cancellationToken);
        }

        if (context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.DotToken) && WalkUpQualifiedNames(context.TargetToken))
        {
            result = GetDottedMembers(position, (QualifiedNameSyntax)context.TargetToken.Parent, context.SemanticModel, memberKindKeyword, cancellationToken);
        }

        if (result.Length > 0)
        {
            return Task.FromResult(result.WhereAsArray(s => MatchesMemberKind(s, memberKindKeyword)));
        }

        return SpecializedTasks.EmptyImmutableArray<ISymbol>();
    }

    private static bool MatchesMemberKind(ISymbol symbol, SyntaxKind memberKindKeyword)
    {
        if (symbol.Kind == SymbolKind.Alias)
        {
            symbol = ((IAliasSymbol)symbol).Target;
        }

        if (symbol is INamespaceOrTypeSymbol)
        {
            return true;
        }

        var method = symbol as IMethodSymbol;
        if (method != null)
        {
            if (!method.ReturnsVoid)
            {
                return memberKindKeyword == SyntaxKind.FunctionKeyword;
            }

            return memberKindKeyword == SyntaxKind.SubKeyword;
        }

        var property = symbol as IPropertySymbol;
        if (property != null)
        {
            return memberKindKeyword == SyntaxKind.PropertyKeyword;
        }

        return memberKindKeyword == SyntaxKind.EventKeyword;
    }

    private static ImmutableArray<ISymbol> GetDottedMembers(int position, QualifiedNameSyntax qualifiedName, SemanticModel semanticModel, SyntaxKind memberKindKeyword, CancellationToken cancellationToken)
    {
        var containingType = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (containingType == null)
        {
            return ImmutableArray<ISymbol>.Empty;
        }

        var unimplementedInterfacesAndMembers = from item in containingType.GetAllUnimplementedMembersInThis(containingType.Interfaces, cancellationToken)
                                                select new { @interface = item.Item1, members = item.Item2.Where(s => MatchesMemberKind(s, memberKindKeyword)) };

        var interfaces = unimplementedInterfacesAndMembers.Where(i => i.members.Any())
                                                            .Select(i => i.@interface);

        var members = unimplementedInterfacesAndMembers.SelectMany(i => i.members);

        var interfacesAndContainers = new HashSet<ISymbol>(interfaces);
        foreach (var @interface in interfaces)
        {
            AddAliasesAndContainers(@interface, interfacesAndContainers, null, null);
        }

        var namespaces = interfacesAndContainers.OfType<INamespaceSymbol>();

        var left = qualifiedName.Left;

        var leftHandTypeInfo = semanticModel.GetTypeInfo(left, cancellationToken);
        var leftHandBinding = semanticModel.GetSymbolInfo(left, cancellationToken);

        INamespaceOrTypeSymbol? container = leftHandTypeInfo.Type;
        if (container == null || container.IsErrorType())
        {
            container = leftHandBinding.Symbol as INamespaceOrTypeSymbol;
        }

        if (container == null)
        {
            container = leftHandBinding.CandidateSymbols.FirstOrDefault() as INamespaceOrTypeSymbol;
        }

        if (container == null)
        {
            return ImmutableArray<ISymbol>.Empty;
        }

        var symbols = semanticModel.LookupSymbols(position, container);

        var hashSet = new HashSet<ISymbol>(symbols.ToArray()
                                   .Where(s => interfacesAndContainers.Contains(s, SymbolEquivalenceComparer.Instance) ||
                                              (s is INamespaceSymbol && namespaces.Contains((INamespaceSymbol)s, INamespaceSymbolExtensions.EqualityComparer)) ||
                                              members.Contains(s)));
        return hashSet.ToImmutableArray();
    }

    private ImmutableArray<ISymbol> InterfaceMemberGetter(ITypeSymbol @interface, ISymbol within)
    {
        return ImmutableArray.CreateRange<ISymbol>(@interface.AllInterfaces.SelectMany(i => i.GetMembers()).Where(s => s.IsAccessibleWithin(within)))
            .AddRange(@interface.GetMembers());
    }

    private ImmutableArray<ISymbol> GetInterfacesAndContainers(int position, SyntaxNode node, SemanticModel semanticModel, SyntaxKind kind, CancellationToken cancellationToken)
    {
        var containingType = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (containingType == null)
        {
            return ImmutableArray<ISymbol>.Empty;
        }

        var interfaceWithUnimplementedMembers = containingType.GetAllUnimplementedMembersInThis(containingType.Interfaces, InterfaceMemberGetter, cancellationToken)
                                            .Where(i => i.Item2.Any(interfaceOrContainer => MatchesMemberKind(interfaceOrContainer, kind)))
                                            .Select(i => i.Item1);

        var interfacesAndContainers = new HashSet<ISymbol>(interfaceWithUnimplementedMembers);
        foreach (var i in interfaceWithUnimplementedMembers)
        {
            AddAliasesAndContainers(i, interfacesAndContainers, node, semanticModel);
        }

        var symbols = new HashSet<ISymbol>(semanticModel.LookupSymbols(position));
        var availableInterfacesAndContainers = interfacesAndContainers.Where(
            interfaceOrContainer => symbols.Contains(interfaceOrContainer.OriginalDefinition)).ToImmutableArray();

        var result = TryAddGlobalTo(availableInterfacesAndContainers);

        // Even if there's not anything left to implement, we'll show the list of interfaces,
        // the global namespace, and the project root namespace (if any), as long as the class implements something.
        if (!result.Any() && containingType.Interfaces.Any())
        {
            var defaultListing = new List<ISymbol>(containingType.Interfaces);
            defaultListing.Add(semanticModel.Compilation.GlobalNamespace);
            if (containingType.ContainingNamespace != null)
            {
                defaultListing.Add(containingType.ContainingNamespace);
                AddAliasesAndContainers(containingType.ContainingNamespace, defaultListing, node, semanticModel);
            }

            return defaultListing.ToImmutableArray();
        }

        return result;
    }

    private static void AddAliasesAndContainers(ISymbol symbol, ICollection<ISymbol> interfacesAndContainers, SyntaxNode? node, SemanticModel? semanticModel)
    {
        // Add aliases, if any for 'symbol'
        AddAlias(symbol, interfacesAndContainers, node, semanticModel);

        // Add containers for 'symbol'
        var containingSymbol = symbol.ContainingSymbol;
        if (containingSymbol != null && !interfacesAndContainers.Contains(containingSymbol))
        {
            interfacesAndContainers.Add(containingSymbol);

            // Add aliases, if any for 'containingSymbol'
            AddAlias(containingSymbol, interfacesAndContainers, node, semanticModel);

            if (!IsGlobal(containingSymbol))
            {
                AddAliasesAndContainers(containingSymbol, interfacesAndContainers, node, semanticModel);
            }
        }
    }

    private static void AddAlias(ISymbol symbol, ICollection<ISymbol> interfacesAndContainers, SyntaxNode? node, SemanticModel? semanticModel)
    {
        if (node != null && semanticModel != null && symbol is INamespaceOrTypeSymbol)
        {
            var aliasSymbol = ((INamespaceOrTypeSymbol)symbol).GetAliasForSymbol(node, semanticModel);
            if (aliasSymbol != null && !interfacesAndContainers.Contains(aliasSymbol))
            {
                interfacesAndContainers.Add(aliasSymbol);
            }
        }
    }

    private static bool IsGlobal(ISymbol containingSymbol)
    {
        var @namespace = containingSymbol as INamespaceSymbol;
        return @namespace != null && @namespace.IsGlobalNamespace;
    }

    private static ImmutableArray<ISymbol> TryAddGlobalTo(ImmutableArray<ISymbol> symbols)
    {
        var withGlobalContainer = symbols.FirstOrDefault(s => s.ContainingNamespace.IsGlobalNamespace);
        if (withGlobalContainer != null)
        {
            return symbols.Concat(ImmutableArray.Create<ISymbol>(withGlobalContainer.ContainingNamespace));
        }

        return symbols;
    }

    private static bool WalkUpQualifiedNames(SyntaxToken token)
    {
        var parent = token.Parent;
        while (parent != null && parent.IsKind(SyntaxKind.QualifiedName))
        {
            parent = parent.Parent;
        }

        return parent != null && parent.IsKind(SyntaxKind.ImplementsClause);
    }

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, VisualBasicSyntaxContext context)
    {
        if (IsGlobal(symbol))
        {
            return ("Global", "", "Global");
        }

        if (IsGenericType(symbol))
        {
            var displayText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position);
            return (displayText, "", displayText);
        }
        else
        {
            return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);
        }
    }

    private static bool IsGenericType(ISymbol symbol)
    {
        return symbol.MatchesKind(SymbolKind.NamedType) && symbol.GetAllTypeArguments().Any();
    }

    private static readonly SymbolDisplayFormat MinimalFormatWithoutGenerics =
        SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    private const string InsertionTextOnOpenParen = nameof(InsertionTextOnOpenParen);

    protected override CompletionItem CreateItem(
        CompletionContext completionContext,
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        VisualBasicSyntaxContext context,
        SupportedPlatformData? supportedPlatformData)
    {
        var item = CreateItemDefault(displayText, displayTextSuffix, insertionText, symbols, context, supportedPlatformData);

        if (IsGenericType(symbols[0].Symbol))
        {
            var text = symbols[0].Symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, MinimalFormatWithoutGenerics);
            item = item.AddProperty(InsertionTextOnOpenParen, text);
        }

        return item;
    }

    protected override string? GetInsertionText(CompletionItem item, char ch)
    {
        if (ch == '(')
        {
            if (item.TryGetProperty(InsertionTextOnOpenParen, out var insertionText))
            {
                return insertionText;
            }
        }

        return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch);
    }
}
