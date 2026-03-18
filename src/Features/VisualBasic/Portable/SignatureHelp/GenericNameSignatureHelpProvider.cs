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

[ExportSignatureHelpProvider("GenericNameSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class GenericNameSignatureHelpProvider() : AbstractVisualBasicSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => [' ', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    private static SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetGenericName(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.TypeArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.TypeArgumentList, position);
        }

        return null;
    }

    private static bool TryGetGenericName(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out GenericNameSyntax? genericName)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out genericName))
        {
            return false;
        }

        return genericName.TypeArgumentList != null;
    }

    private static bool IsTriggerToken(SyntaxToken token)
        => (token.Kind() == SyntaxKind.OfKeyword || token.Kind() == SyntaxKind.CommaToken) &&
            token.Parent is TypeArgumentListSyntax &&
            token.Parent.Parent is GenericNameSyntax;

    private static bool IsArgumentListToken(GenericNameSyntax node, SyntaxToken token)
        => node.TypeArgumentList.Span.Contains(token.SpanStart) &&
            token != node.TypeArgumentList.CloseParenToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetGenericName(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var genericName))
        {
            return null;
        }

        var beforeDotExpression = genericName.IsRightSideOfDotOrBang() ? genericName.GetLeftSideOfDot() : null;
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(beforeDotExpression ?? genericName, cancellationToken).ConfigureAwait(false);

        var leftSymbol = beforeDotExpression == null ? null :
                            semanticModel.GetSymbolInfo(beforeDotExpression, cancellationToken).GetAnySymbol() as INamespaceOrTypeSymbol;
        var leftType = beforeDotExpression == null ? null :
                          semanticModel.GetTypeInfo(beforeDotExpression, cancellationToken).Type as INamespaceOrTypeSymbol;
        var leftContainer = leftSymbol ?? leftType;

        var isBaseAccess = beforeDotExpression is MyBaseExpressionSyntax;
        var namespacesOrTypesOnly = SyntaxFacts.IsInNamespaceOrTypeContext(genericName);
        var includeExtensions = leftSymbol == null && leftType != null;

        var name = genericName.Identifier.ValueText;
        var symbols = isBaseAccess
            ? semanticModel.LookupBaseMembers(position, name)
            : namespacesOrTypesOnly
                ? semanticModel.LookupNamespacesAndTypes(position, leftContainer, name)
                : semanticModel.LookupSymbols(position, leftContainer, name, includeExtensions);

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var accessibleSymbols = symbols.WhereAsArray(s => s.GetArity() > 0)
                                        .WhereAsArray(s => s is INamedTypeSymbol or IMethodSymbol)
                                        .FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                        .Sort(semanticModel, genericName.SpanStart);

        if (accessibleSymbols.Length == 0)
        {
            return null;
        }

        var structuralTypeDisplayService = document.GetLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(genericName.TypeArgumentList);
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

        return CreateSignatureHelpItems(
            accessibleSymbols.Select(s => Convert(s, genericName, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex: null, parameterIndexOverride: -1);
    }

    private static SignatureHelpItem Convert(ISymbol symbol, GenericNameSyntax genericName, SemanticModel semanticModel, IStructuralTypeDisplayService structuralTypeDisplayService, IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = genericName.SpanStart;
        SignatureHelpItem item;
        if (symbol is INamedTypeSymbol namedType)
        {
            item = CreateItem(
                symbol, semanticModel, position,
                structuralTypeDisplayService,
                false,
                symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(namedType, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(),
                namedType.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
        }
        else
        {
            var method = (IMethodSymbol)symbol;
            item = CreateItem(
                symbol, semanticModel, position,
                structuralTypeDisplayService,
                false,
                symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(method, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(method, semanticModel, position),
                method.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
        }

        return item;
    }

    private static readonly SymbolDisplayFormat s_minimallyQualifiedFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions | SymbolDisplayGenericsOptions.IncludeVariance);

    private static SignatureHelpSymbolParameter Convert(ITypeParameterSymbol parameter, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var parts = new List<SymbolDisplayPart>();
        parts.AddRange(parameter.ToMinimalDisplayParts(semanticModel, position, s_minimallyQualifiedFormat));
        AddConstraints(parameter, parts, semanticModel, position);

        return new SignatureHelpSymbolParameter(
            parameter.Name,
            isOptional: false,
            documentationFactory: parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            displayParts: parts);
    }

    private static IList<SymbolDisplayPart> AddConstraints(ITypeParameterSymbol typeParam,
                                    List<SymbolDisplayPart> parts,
                                    SemanticModel semanticModel,
                                    int position)
    {
        var constraintTypes = typeParam.ConstraintTypes;
        var constraintCount = TypeParameterSpecialConstraintCount(typeParam) + constraintTypes.Length;

        if (constraintCount != 0)
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.AsKeyword));
            parts.Add(Space());

            if (constraintCount > 1)
            {
                parts.Add(Punctuation(SyntaxKind.OpenBraceToken));
            }

            var needComma = false;
            if (typeParam.HasReferenceTypeConstraint)
            {
                parts.Add(Keyword(SyntaxKind.ClassKeyword));
                needComma = true;
            }
            else if (typeParam.HasValueTypeConstraint)
            {
                parts.Add(Keyword(SyntaxKind.StructureKeyword));
                needComma = true;
            }

            foreach (var baseType in constraintTypes)
            {
                if (needComma)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                parts.AddRange(baseType.ToMinimalDisplayParts(semanticModel, position));
                needComma = true;
            }

            if (typeParam.HasConstructorConstraint)
            {
                if (needComma)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                parts.Add(Keyword(SyntaxKind.NewKeyword));
            }

            if (constraintCount > 1)
            {
                parts.Add(Punctuation(SyntaxKind.CloseBraceToken));
            }
        }

        return parts;
    }

    private static int TypeParameterSpecialConstraintCount(ITypeParameterSymbol typeParam)
        => (typeParam.HasReferenceTypeConstraint ? 1 : 0) +
            (typeParam.HasValueTypeConstraint ? 1 : 0) +
            (typeParam.HasConstructorConstraint ? 1 : 0);

    private static IList<SymbolDisplayPart> GetPreambleParts(IMethodSymbol method, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();

        AddExtensionPreamble(method, result);

        var containingType = GetContainingType(method);
        if (containingType != null)
        {
            result.AddRange(containingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.DotToken));
        }

        result.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, method, method.Name));

        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        result.Add(Keyword(SyntaxKind.OfKeyword));
        result.Add(Space());
        return result;
    }

    private static ITypeSymbol? GetContainingType(IMethodSymbol method)
    {
        var result = method.ReceiverType;

        if (result.Kind != SymbolKind.NamedType || !((INamedTypeSymbol)result).IsScriptClass)
        {
            return result;
        }
        else
        {
            return null;
        }
    }

    private static IList<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        result.Add(Punctuation(SyntaxKind.CloseParenToken));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));

        var first = true;
        foreach (var parameter in method.Parameters)
        {
            if (!first)
            {
                result.Add(Punctuation(SyntaxKind.CommaToken));
                result.Add(Space());
            }

            first = false;
            result.AddRange(parameter.ToMinimalDisplayParts(semanticModel, position));
        }

        result.Add(Punctuation(SyntaxKind.CloseParenToken));

        if (!method.ReturnsVoid)
        {
            result.Add(Space());
            result.Add(Keyword(SyntaxKind.AsKeyword));
            result.Add(Space());
            result.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        }

        return result;
    }

    private static IList<SymbolDisplayPart> GetPreambleParts(INamedTypeSymbol namedType, SemanticModel semanticModel, int position)
    {
        var result = new List<SymbolDisplayPart>();
        var format = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
        result.AddRange(namedType.ToMinimalDisplayParts(semanticModel, position, format));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));
        result.Add(Keyword(SyntaxKind.OfKeyword));
        result.Add(Space());
        return result;
    }

    private static IList<SymbolDisplayPart> GetPostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
