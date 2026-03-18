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
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("FunctionAggregationSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class FunctionAggregationSignatureHelpProvider() : AbstractVisualBasicSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['('];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    private static SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetFunctionAggregation(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var functionAggregation) &&
            functionAggregation.SpanStart == currentSpan.Start)
        {
            return new SignatureHelpState(0, 0, null, null);
        }

        return null;
    }

    private static bool TryGetFunctionAggregation(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason,
                                               CancellationToken cancellationToken, out FunctionAggregationSyntax? functionAggregation)
    {
        return CommonSignatureHelpUtilities.TryGetSyntax(
            root,
            position,
            syntaxFacts,
            triggerReason,
            t => t.Parent is FunctionAggregationSyntax,
            (n, t) => n.CloseParenToken != t && n.Span.Contains(t.SpanStart) && n.OpenParenToken.SpanStart <= t.SpanStart,
            cancellationToken,
            out functionAggregation);
    }

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetFunctionAggregation(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var functionAggregation))
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methods = semanticModel.LookupSymbols(
            functionAggregation.SpanStart,
            name: functionAggregation.FunctionName.ValueText,
            includeReducedExtensionMethods: true).OfType<IMethodSymbol>()
                                                  .Where(m => m.IsAggregateFunction())
                                                  .ToImmutableArrayOrEmpty();

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var accessibleMethods = methods.WhereAsArray(m => m.IsAccessibleWithin(within))
                                        .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                        .Sort(semanticModel, functionAggregation.SpanStart);

        if (!accessibleMethods.Any())
        {
            return null;
        }

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(functionAggregation, functionAggregation.SpanStart, n => n.CloseParenToken);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        return CreateSignatureHelpItems(
            accessibleMethods.Select(m => Convert(m, functionAggregation, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex: null, parameterIndexOverride: -1);
    }

    private static SignatureHelpItem Convert(IMethodSymbol method,
                                       FunctionAggregationSyntax functionAggregation,
                                       SemanticModel semanticModel,
                                       IStructuralTypeDisplayService structuralTypeDisplayService,
                                       IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = functionAggregation.SpanStart;
        var item = CreateItem(
            method, semanticModel, position,
            structuralTypeDisplayService,
            false,
            method.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetPreambleParts(method),
            GetSeparatorParts(),
            GetPostambleParts(method, semanticModel, position),
            GetParameterParts(method, semanticModel, position, documentationCommentFormattingService));
        return item;
    }

    private static IList<SymbolDisplayPart> GetPreambleParts(IMethodSymbol method)
    {
        var result = new List<SymbolDisplayPart>();
        AddExtensionPreamble(method, result);
        result.AddMethodName(method.Name);
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        return result;
    }

    private static IList<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method,
                                       SemanticModel semanticModel,
                                       int position)
    {
        var parts = new List<SymbolDisplayPart>();
        parts.Add(Punctuation(SyntaxKind.CloseParenToken));

        if (!method.ReturnsVoid)
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.AsKeyword));
            parts.Add(Space());
            parts.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        }

        return parts;
    }

    private static IList<SignatureHelpSymbolParameter> GetParameterParts(IMethodSymbol method, SemanticModel semanticModel, int position,
                                       IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        // Function <name>() As <type>
        if (method.Parameters.Length != 1)
        {
            return SpecializedCollections.EmptyList<SignatureHelpSymbolParameter>();
        }

        // Function <name>(selector as Func(Of T, R)) As R
        var parameter = method.Parameters[0];
        if (parameter.Type.TypeKind == TypeKind.Delegate)
        {
            var delegateInvokeMethod = ((INamedTypeSymbol)parameter.Type).DelegateInvokeMethod;

            if (delegateInvokeMethod != null &&
               delegateInvokeMethod.Parameters.Length == 1 &&
               !delegateInvokeMethod.ReturnsVoid)
            {
                var parts = new List<SymbolDisplayPart>();
                parts.Add(Text(VBWorkspaceResources.expression));
                parts.Add(Space());
                parts.Add(Keyword(SyntaxKind.AsKeyword));
                parts.Add(Space());
                parts.AddRange(delegateInvokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));

                var sigHelpParameter = new SignatureHelpSymbolParameter(
                    VBWorkspaceResources.expression,
                    parameter.IsOptional,
                    parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    parts);

                return [sigHelpParameter];
            }
        }

        return SpecializedCollections.EmptyList<SignatureHelpSymbolParameter>();
    }
}
