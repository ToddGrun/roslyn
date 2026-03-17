// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(PartialTypeCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(HandlesClauseCompletionProvider))]
[Shared]
internal sealed partial class PartialTypeCompletionProvider : AbstractPartialTypeCompletionProvider<VisualBasicSyntaxContext>
{
    private const string InsertionTextOnOpenParen = nameof(InsertionTextOnOpenParen);

    private static readonly SymbolDisplayFormat _insertionTextFormatWithGenerics =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            genericsOptions:
                SymbolDisplayGenericsOptions.IncludeTypeParameters |
                SymbolDisplayGenericsOptions.IncludeVariance |
                SymbolDisplayGenericsOptions.IncludeTypeConstraints);

    private static readonly SymbolDisplayFormat _displayTextFormat =
        _insertionTextFormatWithGenerics.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PartialTypeCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerChars;

    protected override SyntaxNode? GetPartialTypeSyntaxNode(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        TypeStatementSyntax? statement = null;
        return tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, out statement) ? statement : null;
    }

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(INamedTypeSymbol symbol, VisualBasicSyntaxContext context)
    {
        var displayText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format: _displayTextFormat);
        var insertionText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format: _insertionTextFormatWithGenerics);
        return (displayText, "", insertionText);
    }

    protected override ImmutableArray<KeyValuePair<string, string>> GetProperties(INamedTypeSymbol symbol, VisualBasicSyntaxContext context)
        => [new KeyValuePair<string, string>(InsertionTextOnOpenParen, symbol.Name.EscapeIdentifier())];

    public override async Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        if (ch == '(')
        {
            if (selectedItem.TryGetProperty(InsertionTextOnOpenParen, out var insertionText))
            {
                return new TextChange(selectedItem.Span, insertionText);
            }
        }

        return await base.GetTextChangeAsync(document, selectedItem, ch, cancellationToken).ConfigureAwait(false);
    }
}
