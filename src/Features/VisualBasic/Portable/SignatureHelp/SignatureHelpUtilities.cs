// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal static class SignatureHelpUtilities
{
    private static readonly Func<ArgumentListSyntax, SyntaxToken> s_getArgumentListOpenToken = list => list.OpenParenToken;
    private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListOpenToken = list => list.OpenParenToken;
    private static readonly Func<CollectionInitializerSyntax, SyntaxToken> s_getCollectionInitializerOpenToken = i => i.OpenBraceToken;

    private static readonly Func<ArgumentListSyntax, SyntaxToken> s_getArgumentListCloseToken =
        list =>
        {
            // In the case where the user has typed "Goo(bar:" then the parser doesn't consider
            // the colon part of the signature.  However, we want to as it is clearly part of a
            // named parameter they are in the middle of typing.  So consume it in that case.
            if (list.CloseParenToken.IsMissing)
            {
                var nextToken = list.GetLastToken().GetNextToken();
                var nextNextToken = nextToken.GetNextToken();
                if (nextToken.Kind() == SyntaxKind.ColonToken && nextNextToken.Kind() != SyntaxKind.None)
                {
                    return nextNextToken;
                }
            }

            return list.CloseParenToken;
        };

    private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListCloseToken = list => list.CloseParenToken;
    private static readonly Func<CollectionInitializerSyntax, SyntaxToken> s_getCollectionInitializerCloseToken = i => i.CloseBraceToken;

    private static readonly Func<ArgumentListSyntax, SyntaxNodeOrTokenList> s_getArgumentListArgumentsWithSeparators = list => list.Arguments.GetWithSeparators();
    private static readonly Func<TypeArgumentListSyntax, SyntaxNodeOrTokenList> s_getTypeArgumentListArgumentsWithSeparators = list => list.Arguments.GetWithSeparators();
    private static readonly Func<CollectionInitializerSyntax, SyntaxNodeOrTokenList> s_getCollectionInitializerArgumentsWithSeparators = i => i.Initializers.GetWithSeparators();

    private static readonly Func<ArgumentListSyntax, IEnumerable<string?>> s_getArgumentListNames =
        list => list.Arguments.Select(a =>
                                     {
                                         var simpleArgument = a as SimpleArgumentSyntax;
                                         var value = simpleArgument?.NameColonEquals != null
                                                        ? simpleArgument.NameColonEquals.Name.Identifier.ValueText
                                                        : null;
                                         return string.IsNullOrEmpty(value) ? null : value;
                                     });

    private static readonly Func<TypeArgumentListSyntax, IEnumerable<string?>> s_getTypeArgumentListNames = list => list.Arguments.Select(a => (string?)null);
    private static readonly Func<CollectionInitializerSyntax, IEnumerable<string?>> s_getCollectionInitializerNames = i => i.Initializers.Select(a => (string?)null);

    internal static TextSpan GetSignatureHelpSpan(ArgumentListSyntax argumentList)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getArgumentListCloseToken);

    internal static TextSpan GetSignatureHelpSpan(ArgumentListSyntax argumentList, int start)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, start, s_getArgumentListCloseToken);

    internal static TextSpan GetSignatureHelpSpan(TypeArgumentListSyntax argumentList)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getTypeArgumentListCloseToken);

    internal static TextSpan GetSignatureHelpSpan(CollectionInitializerSyntax initializer)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(initializer, initializer.SpanStart, s_getCollectionInitializerCloseToken);

    internal static SignatureHelpState? GetSignatureHelpState(ArgumentListSyntax argumentList, int position)
        => CommonSignatureHelpUtilities.GetSignatureHelpState(
            argumentList,
            position,
            s_getArgumentListOpenToken,
            s_getArgumentListCloseToken,
            s_getArgumentListArgumentsWithSeparators,
            s_getArgumentListNames);

    internal static SignatureHelpState? GetSignatureHelpState(TypeArgumentListSyntax typeArgumentList, int position)
        => CommonSignatureHelpUtilities.GetSignatureHelpState(
            typeArgumentList,
            position,
            s_getTypeArgumentListOpenToken,
            s_getTypeArgumentListCloseToken,
            s_getTypeArgumentListArgumentsWithSeparators,
            s_getTypeArgumentListNames);

    internal static SignatureHelpState? GetSignatureHelpState(CollectionInitializerSyntax initializer, int position)
        => CommonSignatureHelpUtilities.GetSignatureHelpState(
            initializer,
            position,
            s_getCollectionInitializerOpenToken,
            s_getCollectionInitializerCloseToken,
            s_getCollectionInitializerArgumentsWithSeparators,
            s_getCollectionInitializerNames);
}
