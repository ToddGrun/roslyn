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
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("RaiseEventSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RaiseEventStatementSignatureHelpProvider() : AbstractVisualBasicSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [];

    private static SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetRaiseEventStatement(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var statement) &&
            currentSpan.Start == statement.Name.SpanStart)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(statement.ArgumentList, position);
        }

        return null;
    }

    private static bool TryGetRaiseEventStatement(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out RaiseEventStatementSyntax? statement)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out statement))
        {
            return false;
        }

        return statement.ArgumentList != null;
    }

    private static bool IsTriggerToken(SyntaxToken token)
        => (token.Kind() == SyntaxKind.OpenParenToken || token.Kind() == SyntaxKind.CommaToken) &&
            token.Parent is ArgumentListSyntax &&
            token.Parent.Parent is RaiseEventStatementSyntax;

    private static bool IsArgumentListToken(RaiseEventStatementSyntax statement, SyntaxToken token)
        => statement.ArgumentList != null &&
            statement.ArgumentList.Span.Contains(token.SpanStart) &&
            statement.ArgumentList.CloseParenToken != token;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        MemberDisplayOptions options,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetRaiseEventStatement(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var raiseEventStatement))
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var containingType = semanticModel.GetEnclosingSymbol(position, cancellationToken).ContainingType;

        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

        var events = syntaxFactsService.IsInStaticContext(raiseEventStatement)
            ? semanticModel.LookupStaticMembers(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText)
            : semanticModel.LookupSymbols(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText);

        var allowedEvents = events.WhereAsArray(s => s.Kind == SymbolKind.Event && Equals(s.ContainingType, containingType))
                                   .OfType<IEventSymbol>()
                                   .ToImmutableArrayOrEmpty()
                                   .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                   .Sort(semanticModel, raiseEventStatement.SpanStart);

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(raiseEventStatement.ArgumentList, raiseEventStatement.Name.SpanStart);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        return CreateSignatureHelpItems(
            allowedEvents.Select(e => Convert(e, raiseEventStatement, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex: null, parameterIndexOverride: -1);
    }

    private static SignatureHelpItem Convert(
        IEventSymbol eventSymbol,
        RaiseEventStatementSyntax raiseEventStatement,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = raiseEventStatement.SpanStart;

        var type = (INamedTypeSymbol)eventSymbol.Type;

        var item = CreateItem(
            eventSymbol, semanticModel, position,
            structuralTypeDisplayService,
            false,
            eventSymbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetPreambleParts(eventSymbol, semanticModel, position),
            GetSeparatorParts(),
            GetPostambleParts(),
            type.DelegateInvokeMethod.GetParameters().Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());

        return item;
    }

    private static IList<SymbolDisplayPart> GetPreambleParts(
        IEventSymbol eventSymbol,
        SemanticModel semanticModel,
        int position)
    {
        var result = new List<SymbolDisplayPart>();

        result.AddRange(eventSymbol.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.DotToken));

        var format = MinimallyQualifiedWithoutParametersFormat;
        format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeContainingType);
        format = format.RemoveKindOptions(SymbolDisplayKindOptions.IncludeMemberKeyword);

        result.AddRange(eventSymbol.ToMinimalDisplayParts(semanticModel, position, format));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));

        return result;
    }

    private static IList<SymbolDisplayPart> GetPostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
