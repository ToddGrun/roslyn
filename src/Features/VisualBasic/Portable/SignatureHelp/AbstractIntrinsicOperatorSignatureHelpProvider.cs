// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

internal abstract class AbstractIntrinsicOperatorSignatureHelpProvider<TSyntaxNode> : AbstractVisualBasicSignatureHelpProvider
    where TSyntaxNode : SyntaxNode
{
    protected abstract bool IsTriggerToken(SyntaxToken token);
    protected abstract bool IsArgumentListToken(TSyntaxNode node, SyntaxToken token);
    protected abstract ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(TSyntaxNode node, Document document, CancellationToken cancellationToken);

    private bool TryGetSyntaxNode(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out TSyntaxNode? node)
    {
        return CommonSignatureHelpUtilities.TryGetSyntax(
            root,
            position,
            syntaxFacts,
            triggerReason,
            IsTriggerToken,
            IsArgumentListToken,
            cancellationToken,
            out node);
    }

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetSyntaxNode(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var node))
        {
            return null;
        }

        var items = new List<SignatureHelpItem>();

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(node, cancellationToken).ConfigureAwait(false);
        foreach (var documentation in await GetIntrinsicOperatorDocumentationAsync(node, document, cancellationToken).ConfigureAwait(false))
        {
            var signatureHelpItem = GetSignatureHelpItemForIntrinsicOperator(document, semanticModel, node.SpanStart, documentation, cancellationToken);
            items.Add(signatureHelpItem);
        }

        var textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(node, node.SpanStart, n => n.ChildTokens().FirstOrDefault(c => c.Kind() == SyntaxKind.CloseParenToken));
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        return CreateSignatureHelpItems(
            items, textSpan,
            GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex: null, parameterIndexOverride: -1);
    }

    internal static SignatureHelpItem GetSignatureHelpItemForIntrinsicOperator(Document document, SemanticModel semanticModel, int position, AbstractIntrinsicOperatorDocumentation documentation, CancellationToken cancellationToken)
    {
        var parameters = new List<SignatureHelpSymbolParameter>();

        for (var i = 0; i < documentation.ParameterCount; i++)
        {
            var capturedIndex = i;
            parameters.Add(
                new SignatureHelpSymbolParameter(
                    name: documentation.GetParameterName(i),
                    isOptional: false,
                    documentationFactory: c => documentation.GetParameterDocumentation(capturedIndex).ToSymbolDisplayParts().ToTaggedText(),
                    displayParts: documentation.GetParameterDisplayParts(i)));
        }

        var suffixParts = documentation.GetSuffix(semanticModel, position, null, cancellationToken);

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();

        return CreateItem(
            null, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic: false,
            documentationFactory: c => SpecializedCollections.SingletonEnumerable(new TaggedText(TextTags.Text, documentation.DocumentationText)),
            prefixParts: documentation.PrefixParts,
            separatorParts: GetSeparatorParts(),
            suffixParts: suffixParts,
            parameters: parameters);
    }

    protected virtual SignatureHelpState? GetCurrentArgumentStateWorker(SyntaxNode node, int position)
    {
        var commaTokens = new List<SyntaxToken>();
        commaTokens.AddRange(node.ChildTokens().Where(token => token.Kind() == SyntaxKind.CommaToken));

        // Also get any leading skipped tokens on the next token after this node
        var nextToken = node.GetLastToken().GetNextToken();

        foreach (var leadingTrivia in nextToken.LeadingTrivia)
        {
            if (leadingTrivia.Kind() == SyntaxKind.SkippedTokensTrivia)
            {
                commaTokens.AddRange(leadingTrivia.GetStructure().ChildTokens().Where(token => token.Kind() == SyntaxKind.CommaToken));
            }
        }

        // Count how many commas are before us
        return new SignatureHelpState(
            SemanticParameterIndex: commaTokens.Where(token => token.SpanStart < position).Count(),
            SyntacticArgumentCount: commaTokens.Count + 1,
            ArgumentName: null,
            ArgumentNames: null);
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetSyntaxNode(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var node) &&
            currentSpan.Start == node.SpanStart)
        {
            return GetCurrentArgumentStateWorker(node, position);
        }

        return null;
    }
}
