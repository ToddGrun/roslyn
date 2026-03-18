// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.VisualBasic.TaskList;

[ExportLanguageService(typeof(ITaskListService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VisualBasicTaskListService() : AbstractTaskListService
{
    protected override void AppendTaskListItems(
        ImmutableArray<TaskListItemDescriptor> commentDescriptors,
        SyntacticDocument document,
        SyntaxTrivia trivia,
        ArrayBuilder<TaskListItem> items)
    {
        if (PreprocessorHasComment(trivia))
        {
            var commentTrivia = trivia.GetStructure().DescendantTrivia().First(t => t.RawKind == (int)SyntaxKind.CommentTrivia);

            AppendTaskListItemsOnSingleLine(commentDescriptors, document, commentTrivia.ToFullString(), commentTrivia.FullSpan.Start, items);
            return;
        }

        if (IsSingleLineComment(trivia))
        {
            ProcessMultilineComment(commentDescriptors, document, trivia, postfixLength: 0, items);
            return;
        }

        throw ExceptionUtilities.Unreachable();
    }

    protected override string GetNormalizedText(string message)
    {
        return SyntaxFacts.MakeHalfWidthIdentifier(message);
    }

    protected override bool IsIdentifierCharacter(char ch)
    {
        return SyntaxFacts.IsIdentifierPartCharacter(ch);
    }

    protected override int GetCommentStartingIndex(string message)
    {
        // 3 for REM
        var index = GetFirstCharacterIndex(message);
        if (index >= message.Length ||
                   index > message.Length - 3)
        {
            return index;
        }

        var remText = message.Substring(index, "REM".Length);
        if (SyntaxFacts.GetKeywordKind(remText) == SyntaxKind.REMKeyword)
        {
            return GetFirstCharacterIndex(message, index + remText.Length);
        }

        return index;
    }

    private static int GetFirstCharacterIndex(string message, int start = 0)
    {
        var index = GetFirstNonWhitespace(message, start);

        var singleQuote = 0;
        for (var i = index; i < message.Length; i++)
        {
            if (IsSingleQuote(message[i]) && singleQuote < 3)
            {
                singleQuote = singleQuote + 1;
            }
            else
            {
                if (singleQuote == 1 || singleQuote == 3)
                {
                    return GetFirstNonWhitespace(message, i);
                }
                else
                {
                    return index;
                }
            }
        }

        return message.Length;
    }

    private static int GetFirstNonWhitespace(string message, int start)
    {
        for (var i = start; i < message.Length; i++)
        {
            if (!SyntaxFacts.IsWhitespace(message[i]))
            {
                return i;
            }
        }

        return message.Length;
    }

    protected override bool IsMultilineComment(SyntaxTrivia trivia)
    {
        // vb doesn't have multiline comment
        return false;
    }

    protected override bool IsSingleLineComment(SyntaxTrivia trivia)
    {
        return trivia.RawKind == (int)SyntaxKind.CommentTrivia || trivia.RawKind == (int)SyntaxKind.DocumentationCommentTrivia;
    }

    protected override bool PreprocessorHasComment(SyntaxTrivia trivia)
    {
        return SyntaxFacts.IsPreprocessorDirective((SyntaxKind)trivia.RawKind) &&
                       trivia.GetStructure().DescendantTrivia().Any(t => t.RawKind == (int)SyntaxKind.CommentTrivia);
    }

    // TODO: remove this if SyntaxFacts.IsSingleQuote become public
    private const char s_DWCH_SQ = '\uFF07';      // DW single quote
    private const char s_DWCH_LSMART_Q = '\u2018';      // DW left single smart quote
    private const char s_DWCH_RSMART_Q = '\u2019';      // DW right single smart quote

    private static bool IsSingleQuote(char c)
    {
        // Besides the half width and full width ', we also check for Unicode
        // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
        // IME editors paste them in. This isn't really technically correct
        // because we ignore the left-ness or right-ness, but see VS 170991
        return c == '\'' || (c >= s_DWCH_LSMART_Q && (c == s_DWCH_SQ || c == s_DWCH_LSMART_Q || c == s_DWCH_RSMART_Q));
    }
}
