// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(ObjectCreationCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(ObjectInitializerCompletionProvider))]
[Shared]
internal sealed partial class ObjectCreationCompletionProvider : AbstractObjectCreationCompletionProvider<VisualBasicSyntaxContext>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ObjectCreationCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerChar;

    protected override SyntaxNode? GetObjectCreationNewExpression(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        SyntaxNode? newExpression = null;

        if (tree != null && !tree.IsInNonUserCode(position, cancellationToken) && !tree.IsInSkippedText(position, cancellationToken))
        {
            var newToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            newToken = newToken.GetPreviousTokenIfTouchingWord(position);

            // Only after 'new'.
            if (newToken.Kind() == SyntaxKind.NewKeyword)
            {
                // Only if the 'new' belongs to an object creation expression.
                if (tree.IsObjectCreationTypeContext(position, cancellationToken))
                {
                    newExpression = newToken.Parent as ExpressionSyntax;
                }
            }
        }

        return newExpression;
    }

    private static readonly CompletionItemRules s_rules =
        CompletionItemRules.Create(
            commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(')),
            matchPriority: MatchPriority.Preselect,
            selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    protected override CompletionItemRules GetCompletionItemRules(ImmutableArray<SymbolAndSelectionInfo> symbols)
        => s_rules;

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, VisualBasicSyntaxContext context)
    {
        var displayString = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position);
        return (displayString, "", displayString);
    }
}
