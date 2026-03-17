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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(SymbolCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(AwaitCompletionProvider))]
[Shared]
internal sealed class SymbolCompletionProvider : AbstractRecommendationServiceBasedCompletionProvider<VisualBasicSyntaxContext>
{
    private static readonly Dictionary<(bool importDirective, bool preselect, bool tuple), CompletionItemRules> s_cachedRules = [];

    static SymbolCompletionProvider()
    {
        for (var importDirective = 0; importDirective <= 1; importDirective++)
        {
            for (var preselect = 0; preselect <= 1; preselect++)
            {
                for (var tuple = 0; tuple <= 1; tuple++)
                {
                    var context = (importDirective: importDirective == 1, preselect: preselect == 1, tuple: tuple == 1);
                    s_cachedRules[context] = MakeRule(context);
                }
            }
        }
    }

    private static CompletionItemRules MakeRule((bool importDirective, bool preselect, bool tuple) context)
    {
        // '(' should not filter the completion list, even though it's in generic items like IList(Of...)
        var generalBaseline = CompletionItemRules.Default
            .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '('))
            .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '('));

        var importDirectBasline = CompletionItemRules.Create(
            commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.')]);

        var rule = context.importDirective ? importDirectBasline : generalBaseline;

        if (context.preselect)
        {
            rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
        }

        if (context.tuple)
        {
            rule = rule.WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));
        }

        return rule;
    }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SymbolCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    protected override CompletionItemSelectionBehavior PreselectedItemSelectionBehavior => CompletionItemSelectionBehavior.SoftSelection;

    protected override Task<bool> ShouldPreselectInferredTypesAsync(CompletionContext completionContext, int position, CompletionOptions options, CancellationToken cancellationToken)
        => SpecializedTasks.True;

    protected override Task<bool> ShouldProvideAvailableSymbolsInCurrentContextAsync(CompletionContext completionContext, VisualBasicSyntaxContext syntaxContext, int position, CompletionOptions options, CancellationToken cancellationToken)
        => SpecializedTasks.True;

    protected override string GetInsertionText(CompletionItem item, char ch)
        => GetInsertionTextAtInsertionTime(item, ch);

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => IsDefaultTriggerCharacterOrParen(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharsAndParen;

    protected override bool IsTriggerOnDot(SyntaxToken token, int characterPositoin)
    {
        if (token.Kind() != SyntaxKind.DotToken)
        {
            return false;
        }

        var previousToken = token.GetPreviousToken();
        if (previousToken.Kind() == SyntaxKind.IntegerLiteralToken)
        {
            return token.Parent.Kind() != SyntaxKind.SimpleMemberAccessExpression ||
                !((MemberAccessExpressionSyntax)token.Parent).Expression.IsKind(SyntaxKind.NumericLiteralExpression);
        }

        return true;
    }

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, VisualBasicSyntaxContext context)
        => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

    protected override string GetFilterText(ISymbol symbol, string displayText, VisualBasicSyntaxContext context)
    {
        // Filter on New if we have a ctor
        if (symbol.IsConstructor())
        {
            return "New";
        }

        return GetFilterTextDefault(symbol, displayText, context);
    }

    protected override CompletionItemRules GetCompletionItemRules(ImmutableArray<SymbolAndSelectionInfo> symbols, VisualBasicSyntaxContext context)
    {
        var preselect = symbols.Any(s => s.Preselect);
        return s_cachedRules.TryGetValue((context.IsInImportsDirective, preselect, context.IsPossibleTupleContext), out var rule)
            ? rule
            : CompletionItemRules.Default;
    }

    protected override bool IsInstrinsic(ISymbol s)
        => (s as ITypeSymbol)?.IsIntrinsicType() ?? false;
}
