// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider(nameof(CollectionInitializerSignatureHelpProvider), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CollectionInitializerSignatureHelpProvider() : AbstractOrdinaryMethodSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['{', ','];

    public override ImmutableArray<char> RetriggerCharacters => ['}'];

    private bool TryGetInitializerExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out CollectionInitializerSyntax? expression)
        => CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsInitializerExpressionToken, cancellationToken, out expression) &&
           expression != null;

    private bool IsTriggerToken(SyntaxToken token)
        => !token.IsKind(SyntaxKind.None) &&
           token.ValueText.Length == 1 &&
           TriggerCharacters.Contains(token.ValueText[0]) &&
           token.Parent is CollectionInitializerSyntax;

    private static bool IsInitializerExpressionToken(CollectionInitializerSyntax expression, SyntaxToken token)
        => expression.Span.Contains(token.SpanStart) && token != expression.CloseBraceToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetInitializerExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var collectionInitializer))
        {
            return null;
        }

        var addMethods = await CommonSignatureHelpUtilities.GetCollectionInitializerAddMethodsAsync(
            document, collectionInitializer.Parent, options, cancellationToken).ConfigureAwait(false);
        if (addMethods.IsDefaultOrEmpty)
        {
            return null;
        }

        var textSpan = GetSignatureHelpSpan(collectionInitializer);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return CreateCollectionInitializerSignatureHelpItems(
            addMethods.Select(s => ConvertMemberGroupMember(document, s, collectionInitializer.OpenBraceToken.SpanStart, semanticModel)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetInitializerExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out var expression) &&
                currentSpan.Start == GetSignatureHelpSpan(expression).Start)
        {
            return GetSignatureHelpState(expression, position);
        }

        return null;
    }
}
