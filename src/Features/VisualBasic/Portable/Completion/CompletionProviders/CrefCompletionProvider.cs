// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(CrefCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(PartialTypeCompletionProvider))]
[Shared]
internal partial class CrefCompletionProvider : AbstractCrefCompletionProvider
{
    private static readonly SymbolDisplayFormat s_crefFormat =
        new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_minimalParameterTypeFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

    private Action<SyntaxNode>? _testSpeculativeNodeCallbackOpt;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options);

    internal override string Language => LanguageNames.VisualBasic;

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerChars;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CrefCompletionProvider()
    {
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.GetTargetToken(position, cancellationToken);

            if (IsCrefTypeParameterContext(token))
            {
                return;
            }

            // To get a Speculative SemanticModel (which is much faster), we need to
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.Parent?.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
            _testSpeculativeNodeCallbackOpt?.Invoke(parentNode);
            if (parentNode == null)
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(parentNode, cancellationToken).ConfigureAwait(false);

            var symbols = GetSymbols(token, semanticModel, cancellationToken);
            if (!symbols.Any())
            {
                return;
            }

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var items = CreateCompletionItems(semanticModel, symbols, position);
            context.AddItems(items);

            if (IsFirstCrefParameterContext(token))
            {
                // Include Of in case they're typing a type parameter
                context.AddItem(CreateOfCompletionItem());
            }

            context.IsExclusive = true;
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
        {
            // nop
        }
    }

    protected override async Task<(SyntaxToken, SemanticModel, ImmutableArray<ISymbol>)> GetSymbolsAsync(Document document, int position, CompletionOptions options, CancellationToken cancellationToken)
    {
        var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var token = tree.GetTargetToken(position, cancellationToken);

        if (IsCrefTypeParameterContext(token))
        {
            return default;
        }

        // To get a Speculative SemanticModel (which is much faster), we need to
        // walk up to the node the DocumentationTrivia is attached to.
        var parentNode = token.Parent?.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
        _testSpeculativeNodeCallbackOpt?.Invoke(parentNode);
        if (parentNode == null)
        {
            return default;
        }

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(parentNode, cancellationToken).ConfigureAwait(false);

        var symbols = GetSymbols(token, semanticModel, cancellationToken);
        return (token, semanticModel, symbols.ToImmutableArray());
    }

    private static bool IsCrefTypeParameterContext(SyntaxToken token)
        => (token.IsChildToken<TypeArgumentListSyntax>(t => t.OfKeyword) ||
            token.IsChildSeparatorToken<TypeArgumentListSyntax, TypeSyntax>(t => t.Arguments)) &&
            token.Parent?.FirstAncestorOrSelf<XmlCrefAttributeSyntax>() != null;

    private static bool IsCrefStartContext(SyntaxToken token)
    {
        // cases:
        //   <see cref="x|
        //   <see cref='x|
        if (token.IsChildToken<XmlCrefAttributeSyntax>(x => x.StartQuoteToken))
        {
            return true;
        }

        // cases:
        //   <see cref="|
        //   <see cref='|
        if (token.Parent.IsKind(SyntaxKind.XmlString) && token.Parent.IsParentKind(SyntaxKind.XmlAttribute))
        {
            var xmlAttribute = (XmlAttributeSyntax)token.Parent.Parent;
            var xmlName = xmlAttribute.Name as XmlNameSyntax;
            var xmlValue = xmlAttribute.Value as XmlStringSyntax;

            if (xmlName?.LocalName.ValueText == "cref" && xmlValue?.StartQuoteToken == token)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCrefParameterListContext(SyntaxToken token)
    {
        // cases:
        //   <see cref="M(|
        //   <see cref="M(x, |
        return IsFirstCrefParameterContext(token) ||
               token.IsChildSeparatorToken<CrefSignatureSyntax, TypeSyntax>(x => x.ArgumentTypes);
    }

    private static bool IsFirstCrefParameterContext(SyntaxToken token)
        => token.IsChildToken<CrefSignatureSyntax>(x => x.OpenParenToken);

    private static IEnumerable<ISymbol> GetSymbols(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (IsCrefStartContext(token))
        {
            return semanticModel.LookupSymbols(token.SpanStart);
        }
        else if (IsCrefParameterListContext(token))
        {
            return semanticModel.LookupNamespacesAndTypes(token.SpanStart);
        }
        else if (token.IsChildToken<QualifiedNameSyntax>(x => x.DotToken))
        {
            return GetQualifiedSymbols((QualifiedNameSyntax)token.Parent, token, semanticModel, cancellationToken);
        }

        return SpecializedCollections.EmptyEnumerable<ISymbol>();
    }

    private static IEnumerable<ISymbol> GetQualifiedSymbols(QualifiedNameSyntax qualifiedName, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var leftSymbol = semanticModel.GetSymbolInfo(qualifiedName.Left, cancellationToken).Symbol;
        var leftType = semanticModel.GetTypeInfo(qualifiedName.Left, cancellationToken).Type;

        var container = (leftSymbol ?? leftType) as INamespaceOrTypeSymbol;

        foreach (var symbol in semanticModel.LookupSymbols(token.SpanStart, container))
        {
            yield return symbol;
        }

        var namedTypeContainer = container as INamedTypeSymbol;
        if (namedTypeContainer != null)
        {
            foreach (var constructor in namedTypeContainer.Constructors)
            {
                if (!constructor.IsStatic)
                {
                    yield return constructor;
                }
            }
        }
    }

    private static IEnumerable<CompletionItem> CreateCompletionItems(
        SemanticModel semanticModel,
        IEnumerable<ISymbol> symbols,
        int position)
    {
        var builder = SharedPools.Default<StringBuilder>().Allocate();
        try
        {
            foreach (var symbol in symbols)
            {
                builder.Clear();
                yield return CreateCompletionItem(semanticModel, symbol, position, builder);
            }
        }
        finally
        {
            SharedPools.Default<StringBuilder>().ClearAndFree(builder);
        }
    }

    private static CompletionItem CreateCompletionItem(
        SemanticModel semanticModel,
        ISymbol symbol,
        int position,
        StringBuilder builder)
    {
        if (symbol.IsUserDefinedOperator())
        {
            builder.Append("Operator ");
        }

        builder.Append(symbol.ToDisplayString(s_crefFormat));

        var parameters = symbol.GetParameters();

        if (!parameters.IsDefaultOrEmpty)
        {
            builder.Append('(');

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                var parameter = parameters[i];

                if (parameter.RefKind == RefKind.Ref)
                {
                    builder.Append("ByRef ");
                }

                builder.Append(parameter.Type.ToMinimalDisplayString(semanticModel, position, s_minimalParameterTypeFormat));
            }

            builder.Append(')');
        }

        var displayString = builder.ToString();

        return SymbolCompletionItem.CreateWithNameAndKind(
            displayText: displayString,
            displayTextSuffix: "",
            insertionText: null,
            symbols: ImmutableArray.Create(symbol),
            contextPosition: position,
            rules: GetRules(displayString));
    }

    private static CompletionItem CreateOfCompletionItem()
        => CommonCompletionItem.Create(
            "Of", displayTextSuffix: "", CompletionItemRules.Default, Glyph.Keyword,
            description: RecommendedKeyword.CreateDisplayParts("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure));

    private static readonly CharacterSetModificationRule s_WithoutOpenParen = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '(');

    private static readonly CompletionItemRules s_defaultRules = CompletionItemRules.Default;

    private static CompletionItemRules GetRules(string displayText)
    {
        var commitRules = s_defaultRules.CommitCharacterRules;

        if (displayText.Contains("("))
        {
            commitRules = commitRules.Add(s_WithoutOpenParen);
        }

        return s_defaultRules.WithCommitCharacterRules(commitRules);
    }

    internal TestAccessor GetTestAccessor()
        => new TestAccessor(this);

    internal readonly struct TestAccessor
    {
        private readonly CrefCompletionProvider _crefCompletionProvider;

        public TestAccessor(CrefCompletionProvider crefCompletionProvider)
        {
            _crefCompletionProvider = crefCompletionProvider;
        }

        public void SetSpeculativeNodeCallback(Action<SyntaxNode> value)
        {
            _crefCompletionProvider._testSpeculativeNodeCallbackOpt = value;
        }
    }
}
