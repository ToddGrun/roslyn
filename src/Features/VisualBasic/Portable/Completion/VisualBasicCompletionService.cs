// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion;

internal partial class VisualBasicCompletionService : CommonCompletionService
{
    [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.VisualBasic), Shared]
    internal class Factory : ILanguageServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _listenerProvider = listenerProvider;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new VisualBasicCompletionService(languageServices.LanguageServices.SolutionServices, _listenerProvider);
    }

    private CompletionRules _latestRules = CompletionRules.Create(
        dismissIfEmpty: true,
        dismissIfLastCharacterDeleted: true,
        defaultCommitCharacters: CompletionRules.Default.DefaultCommitCharacters,
        defaultEnterKeyRule: EnterKeyRule.Always);

    private VisualBasicCompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider)
        : base(services, listenerProvider)
    {
    }

    public override string Language => LanguageNames.VisualBasic;

    internal override CompletionRules GetRules(CompletionOptions options)
    {
        // Although EnterKeyBehavior is a per-language setting, the meaning of an unset setting (Default) differs between C# and VB
        // In VB the default means Always to maintain previous behavior
        var enterRule = options.EnterKeyBehavior;
        var snippetsRule = options.SnippetsBehavior;

        if (enterRule == EnterKeyRule.Default)
        {
            enterRule = EnterKeyRule.Always;
        }

        if (snippetsRule == SnippetsRule.Default)
        {
            snippetsRule = SnippetsRule.IncludeAfterTypingIdentifierQuestionTab;
        }

        var newRules = _latestRules.WithDefaultEnterKeyRule(enterRule)
                                    .WithSnippetsRule(snippetsRule);

        Interlocked.Exchange(ref _latestRules, newRules);

        return newRules;
    }

    protected override CompletionItem GetBetterItem(CompletionItem item, CompletionItem existingItem)
    {
        // If one is a keyword, and the other is some other item that inserts the same text as the keyword,
        // keep the keyword (VB only), unless the other item is preselected
        if (IsKeywordItem(existingItem) && existingItem.Rules.MatchPriority >= item.Rules.MatchPriority)
        {
            return existingItem;
        }

        return base.GetBetterItem(item, existingItem);
    }

    protected override bool ItemsMatch(CompletionItem item, CompletionItem existingItem)
    {
        if (!base.ItemsMatch(item, existingItem))
        {
            return false;
        }

        // DevDiv 957450 Normally, we want to show items with the same display text and
        // different glyphs. That way, the we won't hide user-defined symbols that happen
        // to match a keyword (like Select). However, we want to avoid showing the keyword
        // for an intrinsic right next to the item for the corresponding symbol.
        // Therefore, if a keyword claims to represent an "intrinsic" item, we'll ignore
        // the glyph when matching.

        var keywordCompletionItem = IsKeywordItem(existingItem) ? existingItem : (IsKeywordItem(item) ? item : null);
        if (keywordCompletionItem != null && keywordCompletionItem.Tags.Contains(WellKnownTags.Intrinsic))
        {
            var otherItem = keywordCompletionItem == item ? existingItem : item;
            var changeText = GetChangeText(otherItem);
            if (changeText == keywordCompletionItem.DisplayText)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return item.Tags == existingItem.Tags || item.Tags.SequenceEqual(existingItem.Tags);
    }

    private string GetChangeText(CompletionItem item)
    {
        var provider = GetProvider(item, project: null) as CommonCompletionProvider;
        if (provider != null)
        {
            // TODO: Document is not available in this code path.. what about providers that need to reconstruct information before producing text?
            var result = provider.GetTextChangeAsync(null, item, null, CancellationToken.None).Result;
            if (result != null)
            {
                return result.Value.NewText;
            }
        }

        return item.DisplayText;
    }

    public override TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition)
        => CompletionUtilities.GetCompletionItemSpan(text, caretPosition);

    internal override bool SupportsTriggerOnDeletion(CompletionOptions options)
    {
        // If the option is null (i.e. default) or 'true', then we want to trigger completion.
        // Only if the option is false do we not want to trigger.
        return options.TriggerOnDeletion == false ? false : true;
    }
}
