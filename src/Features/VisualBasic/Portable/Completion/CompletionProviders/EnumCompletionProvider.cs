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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(EnumCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(ObjectCreationCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class EnumCompletionProvider() : AbstractSymbolCompletionProvider<VisualBasicSyntaxContext>
{
    private static readonly CompletionItemRules s_enumMemberCompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect);

    internal override string Language => LanguageNames.VisualBasic;

    protected override Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext completionContext,
        VisualBasicSyntaxContext syntaxContext,
        int position,
        CompletionOptions options,
        CancellationToken cancellationToken)
    {
        var builder = ArrayBuilder<SymbolAndSelectionInfo>.GetInstance();
        try
        {
            if (syntaxContext.SyntaxTree.IsInNonUserCode(syntaxContext.Position, cancellationToken))
            {
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
            }

            // This providers provides fully qualified names, eg "DayOfWeek.Monday"
            // Don't run after dot because SymbolCompletionProvider will provide
            // members in situations like Dim x = DayOfWeek.$$
            if (syntaxContext.TargetToken.IsKind(SyntaxKind.DotToken))
            {
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
            }

            var typeInferenceService = syntaxContext.GetLanguageService<ITypeInferenceService>();
            var enumType = typeInferenceService.InferType(syntaxContext.SemanticModel, position, objectAsDefault: true, cancellationToken: cancellationToken);

            if (enumType.TypeKind != TypeKind.Enum)
            {
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
            }

            builder.Add(new SymbolAndSelectionInfo(enumType, Preselect: false));

            foreach (var member in enumType.GetMembers())
            {
                if (member.Kind == SymbolKind.Field && ((IFieldSymbol)member).IsConst && member.IsEditorBrowsable(options.MemberDisplayOptions.HideAdvancedMembers, syntaxContext.SemanticModel.Compilation))
                {
                    builder.Add(new SymbolAndSelectionInfo(member, Preselect: true));
                }
            }

            return Task.FromResult(builder.ToImmutable());
        }
        finally
        {
            builder.Free();
        }
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return text[characterPosition] == ' ' ||
            text[characterPosition] == '(' ||
            (characterPosition > 1 && text[characterPosition] == '=' && text[characterPosition - 1] == ':') ||
            SyntaxFacts.IsIdentifierStartCharacter(text[characterPosition]) &&
            options.TriggerOnTypingLetters;
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create(' ', '(', '=');

    // PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
    private readonly object _gate = new();
    private INamedTypeSymbol? _cachedDisplayAndInsertionTextContainingType;
    private VisualBasicSyntaxContext? _cachedDisplayAndInsertionTextContext;
    private string? _cachedDisplayAndInsertionTextContainingTypeText;

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, VisualBasicSyntaxContext context)
    {
        if (symbol.Kind != SymbolKind.Field)
        {
            return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);
        }

        // Completion service allows concurrent calls
        lock (_gate)
        {
            if (!Equals(_cachedDisplayAndInsertionTextContainingType, symbol.ContainingType) || _cachedDisplayAndInsertionTextContext != context)
            {
                var displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithLocalOptions(SymbolDisplayLocalOptions.None);
                _cachedDisplayAndInsertionTextContainingTypeText = symbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat);
                _cachedDisplayAndInsertionTextContainingType = symbol.ContainingType;
                _cachedDisplayAndInsertionTextContext = context;
            }

            var text = _cachedDisplayAndInsertionTextContainingTypeText + "." + symbol.Name;
            return (text, "", text);
        }
    }

    protected override CompletionItem CreateItem(
        CompletionContext completionContext,
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        VisualBasicSyntaxContext context,
        SupportedPlatformData? supportedPlatformData)
    {
        var preselect = symbols.Any(t => t.Preselect);
        var rules = preselect ? s_enumMemberCompletionItemRules : CompletionItemRules.Default;

        var item = SymbolCompletionItem.CreateWithSymbolId(
            displayText: displayText,
            displayTextSuffix: displayTextSuffix,
            insertionText: insertionText,
            filterText: displayText,
            symbols: symbols.SelectAsArray(t => t.Symbol),
            contextPosition: context.Position,
            sortText: insertionText,
            supportedPlatforms: supportedPlatformData,
            rules: rules);

        // Use member name (w/o enum type name) as additional filter text, which would
        // promote this item during matching when user types member name only, like "Red"
        // instead of "Colors.Empty"
        if (symbols[0].Symbol.Kind == SymbolKind.Field)
        {
            item = item.AddTag(WellKnownTags.TargetTypeMatch).WithAdditionalFilterTexts(ImmutableArray.Create(symbols[0].Symbol.Name));
        }

        return item;
    }

    protected override string? GetInsertionText(CompletionItem item, char ch)
    {
        return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch);
    }
}
