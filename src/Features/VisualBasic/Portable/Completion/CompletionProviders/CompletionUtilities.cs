// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

internal static class CompletionUtilities
{
    private const char UnicodeEllipsis = '\u2026';
    private const string OfSuffix = "(Of";
    private const string GenericSuffix = OfSuffix + " " + UnicodeEllipsis + ")";

    internal static readonly ImmutableHashSet<char> CommonTriggerChars = ImmutableHashSet.Create('.', '[', '#', ' ', '=', '<', '{');

    internal static readonly ImmutableHashSet<char> CommonTriggerCharsAndParen = CommonTriggerChars.Add('(');

    internal static readonly ImmutableHashSet<char> SpaceTriggerChar = CommonTriggerChars.Add(' ');

    public static TextSpan GetCompletionItemSpan(SourceText text, int position)
        => CommonCompletionUtilities.GetWordSpan(
            text, position,
            IsCompletionItemStartCharacter,
            IsCompletionItemCharacter);

    public static bool IsWordStartCharacter(char ch)
        => SyntaxFacts.IsIdentifierStartCharacter(ch);

    private static bool IsWordCharacter(char ch)
        => SyntaxFacts.IsIdentifierStartCharacter(ch) || SyntaxFacts.IsIdentifierPartCharacter(ch);

    private static bool IsCompletionItemStartCharacter(char ch)
        => ch == '#' || ch == '[' || IsWordCharacter(ch);

    private static bool IsCompletionItemCharacter(char ch)
        => ch == ']' || IsWordCharacter(ch);

    public static bool IsDefaultTriggerCharacter(SourceText text, int characterPosition, CompletionOptions options)
    {
        var ch = text[characterPosition];
        if (CommonTriggerChars.Contains(ch))
        {
            return true;
        }

        return IsStartingNewWord(text, characterPosition, options);
    }

    public static bool IsDefaultTriggerCharacterOrParen(SourceText text, int characterPosition, CompletionOptions options)
    {
        var ch = text[characterPosition];

        return ch == '(' ||
               CommonTriggerChars.Contains(ch) ||
               IsStartingNewWord(text, characterPosition, options);
    }

    public static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition, CompletionOptions options)
    {
        // Bring up on space or at the start of a word.
        var ch = text[characterPosition];

        return ch == ' ' || IsStartingNewWord(text, characterPosition, options);
    }

    private static bool IsStartingNewWord(SourceText text, int characterPosition, CompletionOptions options)
    {
        if (!options.TriggerOnTypingLetters)
        {
            return false;
        }

        return CommonCompletionUtilities.IsStartingNewWord(
            text, characterPosition, IsWordStartCharacter, IsWordCharacter);
    }

    public static (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(
        ISymbol symbol,
        SyntaxContext context)
    {
        string? name = null;
        if (!CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context, out name))
        {
            name = symbol.Name;
        }

        var displayText = GetDisplayText(name, symbol);
        var insertionText = GetInsertionText(name, symbol, context);
        var suffix = GetSuffix(symbol);

        return (displayText, suffix, insertionText);
    }

    private static string GetDisplayText(string name, ISymbol symbol)
    {
        if (symbol.IsConstructor())
        {
            return "New";
        }
        else
        {
            return name;
        }
    }

    private static string GetSuffix(ISymbol symbol)
    {
        if (symbol.IsConstructor())
        {
            return "";
        }
        else if (symbol.GetArity() > 0)
        {
            return GenericSuffix;
        }
        else
        {
            return "";
        }
    }

    private static string GetInsertionText(string name, ISymbol symbol, SyntaxContext context)
    {
        name = name.EscapeIdentifier(context.IsRightOfNameSeparator, symbol, context.IsWithinAsyncMethod);

        if (symbol.IsConstructor())
        {
            name = "New";
        }
        else if (symbol.GetArity() > 0)
        {
            name += OfSuffix;
        }

        return name;
    }

    public static string GetInsertionTextAtInsertionTime(CompletionItem item, char ch)
    {
        var insertionText = SymbolCompletionItem.GetInsertionText(item);

        // If this item was generic, customize what we insert depending on if the user typed
        // open paren or not.
        if (ch == '(' && item.DisplayTextSuffix == GenericSuffix)
        {
            return insertionText.Substring(0, insertionText.IndexOf('('));
        }

        if (ch == ']')
        {
            if (insertionText[0] != '[')
            {
                // user is committing with ].  If the item doesn't start with '['
                // then add that to the beginning so [ and ] properly pair up.
                return "[" + insertionText;
            }

            if (insertionText.EndsWith("]"))
            {
                // If the user commits with "]" and the item already ends with "]"
                // then trim "]" off the end so we don't have ]] inserted into the
                // document.
                return insertionText.Substring(0, insertionText.Length - 1);
            }
        }

        return insertionText;
    }
}
