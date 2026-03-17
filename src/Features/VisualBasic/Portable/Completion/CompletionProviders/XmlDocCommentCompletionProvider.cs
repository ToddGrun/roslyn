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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Utilities.DocumentationCommentXmlNames;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(XmlDocCommentCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(OverrideCompletionProvider))]
[Shared]
internal sealed class XmlDocCommentCompletionProvider : AbstractDocCommentCompletionProvider<DocumentationCommentTriviaSyntax>
{
    private static readonly ImmutableArray<string> s_keywordNames;

    static XmlDocCommentCompletionProvider()
    {
        var keywordsBuilder = new List<string>();

        foreach (var keywordKind in SyntaxFacts.GetKeywordKinds())
        {
            keywordsBuilder.Add(SyntaxFacts.GetText(keywordKind));
        }

        s_keywordNames = [.. keywordsBuilder];
    }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public XmlDocCommentCompletionProvider()
        : base(s_defaultRules)
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        var isStartOfTag = text[characterPosition] == '<';
        var isClosingTag = (text[characterPosition] == '/' && characterPosition > 0 && text[characterPosition - 1] == '<');
        var isDoubleQuote = text[characterPosition] == '"';

        return isStartOfTag || isClosingTag || isDoubleQuote ||
               IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ['<', '/', '"', ' '];

    public static SyntaxToken GetPreviousTokenIfTouchingText(SyntaxToken token, int position)
        => token.IntersectsWith(position) && IsText(token)
            ? token.GetPreviousToken(includeSkipped: true)
            : token;

    private static bool IsText(SyntaxToken token)
        => token.IsKind(SyntaxKind.XmlNameToken, SyntaxKind.XmlTextLiteralToken, SyntaxKind.IdentifierToken);

    protected override async Task<IEnumerable<CompletionItem>?> GetItemsWorkerAsync(Document document, int position, CompletionTrigger trigger, CancellationToken cancellationToken)
    {
        try
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);

            var parent = token.GetAncestor<DocumentationCommentTriviaSyntax>();

            if (parent == null)
            {
                return null;
            }

            // If the user is typing in xml text, don't trigger on backspace.
            if (token.IsKind(SyntaxKind.XmlTextLiteralToken) &&
                !token.Parent.IsKind(SyntaxKind.XmlString) &&
                trigger.Kind == CompletionTriggerKind.Deletion)
            {
                return null;
            }

            // Never provide any items inside a cref
            if (token.Parent.IsKind(SyntaxKind.XmlString) && token.Parent.Parent.IsKind(SyntaxKind.XmlAttribute))
            {
                var attribute = (XmlAttributeSyntax)token.Parent.Parent;
                var name = attribute.Name as XmlNameSyntax;
                var value = attribute.Value as XmlStringSyntax;
                if (name?.LocalName.ValueText == CrefAttributeName && token != value?.EndQuoteToken)
                {
                    return null;
                }
            }

            if (token.Parent.GetAncestor<XmlCrefAttributeSyntax>() != null)
            {
                return null;
            }

            var items = new List<CompletionItem>();

            var attachedToken = parent.ParentTrivia.Token;
            if (attachedToken.Kind() == SyntaxKind.None)
            {
                return items;
            }

            var declaration = attachedToken.GetAncestor<DeclarationStatementSyntax>();

            // Maybe we're going to suggest the close tag
            if (token.Kind() == SyntaxKind.LessThanSlashToken)
            {
                return GetCloseTagItem(token);
            }
            else if (token.IsKind(SyntaxKind.XmlNameToken) && token.GetPreviousToken().IsKind(SyntaxKind.LessThanSlashToken))
            {
                return GetCloseTagItem(token.GetPreviousToken());
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(false);
            ISymbol? symbol = null;

            if (declaration != null)
            {
                symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            }

            if (symbol != null)
            {
                // Maybe we're going to do attribute completion
                TryGetAttributes(token, position, items, symbol);
                if (items.Any())
                {
                    return items;
                }
            }

            if (trigger.Kind == CompletionTriggerKind.Insertion &&
                trigger.Character != '"' &&
                trigger.Character != '<')
            {
                // With the use of IsTriggerAfterSpaceOrStartOfWordCharacter, the code below is much
                // too aggressive at suggesting tags, so exit early before degrading the experience
                return items;
            }

            items.AddRange(GetAlwaysVisibleItems());

            var parentElement = token.GetAncestor<XmlElementSyntax>();
            var grandParent = parentElement?.Parent;

            if (grandParent.IsKind(SyntaxKind.XmlElement))
            {
                // Avoid including language keywords when following < Or <text, since these cases should only be
                // attempting to complete the XML name (which for language keywords Is 'see'). The VB parser treats
                // spaces after a < character as trailing whitespace, even if an identifier follows it on the same line.
                // Therefore, the consistent VB experience says we never show keywords for < followed by spaces.
                var xmlNameOnly = token.IsKind(SyntaxKind.LessThanToken) || token.Parent.IsKind(SyntaxKind.XmlName);
                var includeKeywords = !xmlNameOnly;

                items.AddRange(GetNestedItems(symbol, includeKeywords));
                AddXmlElementItems(items, grandParent);
            }
            else if (token.Parent.IsKind(SyntaxKind.XmlText) &&
                     token.Parent.IsParentKind(SyntaxKind.DocumentationCommentTrivia))
            {
                // Top level, without tag:
                //     ''' $$
                items.AddRange(GetTopLevelItems(symbol, parent));
            }
            else if (token.Parent.IsKind(SyntaxKind.XmlText) &&
                     token.Parent.Parent.IsKind(SyntaxKind.XmlElement))
            {
                items.AddRange(GetNestedItems(symbol, includeKeywords: true));
                var xmlElement = token.Parent.Parent;

                AddXmlElementItems(items, xmlElement);
            }
            else if (grandParent.IsKind(SyntaxKind.DocumentationCommentTrivia))
            {
                // Top level, with tag:
                //     ''' <$$
                //     ''' <tag$$
                items.AddRange(GetTopLevelItems(symbol, parent));
            }

            if (token.Parent.IsKind(SyntaxKind.XmlElementStartTag, SyntaxKind.XmlName) &&
               parentElement.IsParentKind(SyntaxKind.XmlElement))
            {
                AddXmlElementItems(items, parentElement.Parent);
            }

            return items;
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }
    }

    private void AddXmlElementItems(List<CompletionItem> items, SyntaxNode xmlElement)
    {
        var startTagName = GetStartTagName(xmlElement);
        if (startTagName == ListElementName)
        {
            items.AddRange(GetListItems());
        }
        else if (startTagName == ListHeaderElementName)
        {
            items.AddRange(GetListHeaderItems());
        }
        else if (startTagName == ItemElementName)
        {
            items.AddRange(GetItemTagItems());
        }
    }

    private IEnumerable<CompletionItem>? GetCloseTagItem(SyntaxToken token)
    {
        var endTag = token.Parent as XmlElementEndTagSyntax;
        if (endTag == null)
        {
            return null;
        }

        var element = endTag.Parent as XmlElementSyntax;
        if (element == null)
        {
            return null;
        }

        var startElement = element.StartTag;
        var name = startElement.Name as XmlNameSyntax;
        if (name == null)
        {
            return null;
        }

        var nameToken = name.LocalName;
        if (!nameToken.IsMissing && nameToken.ValueText.Length > 0)
        {
            return SpecializedCollections.SingletonEnumerable(CreateCompletionItem(nameToken.ValueText, beforeCaretText: nameToken.ValueText + ">", afterCaretText: string.Empty));
        }

        return null;
    }

    private static string GetStartTagName(SyntaxNode element)
        => ((XmlNameSyntax)((XmlElementSyntax)element).StartTag.Name).LocalName.ValueText;

    private void TryGetAttributes(SyntaxToken token,
                                 int position,
                                 List<CompletionItem> items,
                                 ISymbol symbol)
    {
        XmlNameSyntax? tagNameSyntax = null;
        SyntaxList<XmlNodeSyntax> tagAttributes = default;

        var startTagSyntax = token.GetAncestor<XmlElementStartTagSyntax>();
        if (startTagSyntax != null)
        {
            tagNameSyntax = startTagSyntax.Name as XmlNameSyntax;
            tagAttributes = startTagSyntax.Attributes;
        }
        else
        {
            var emptyElementSyntax = token.GetAncestor<XmlEmptyElementSyntax>();
            if (emptyElementSyntax != null)
            {
                tagNameSyntax = emptyElementSyntax.Name as XmlNameSyntax;
                tagAttributes = emptyElementSyntax.Attributes;
            }
        }

        if (tagNameSyntax != null)
        {
            var targetToken = GetPreviousTokenIfTouchingText(token, position);
            var tagName = tagNameSyntax.LocalName.ValueText;

            if (targetToken.IsChildToken<XmlNameSyntax>(n => n.LocalName) && targetToken.Parent == tagNameSyntax)
            {
                // <exception |
                items.AddRange(GetAttributes(token, tagName, tagAttributes));
            }

            // <exception a|
            if (targetToken.IsChildToken<XmlNameSyntax>(n => n.LocalName) && targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute))
            {
                // <exception |
                items.AddRange(GetAttributes(token, tagName, tagAttributes));
            }

            // <exception a=""|
            if ((targetToken.IsChildToken<XmlStringSyntax>(s => s.EndQuoteToken) && targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute)) ||
                targetToken.IsChildToken<XmlNameAttributeSyntax>(a => a.EndQuoteToken) ||
                targetToken.IsChildToken<XmlCrefAttributeSyntax>(a => a.EndQuoteToken))
            {
                items.AddRange(GetAttributes(token, tagName, tagAttributes));
            }

            // <param name="|"
            if ((targetToken.IsChildToken<XmlStringSyntax>(s => s.StartQuoteToken) && targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute)) ||
                targetToken.IsChildToken<XmlNameAttributeSyntax>(a => a.StartQuoteToken))
            {
                string attributeName;

                var xmlAttributeName = targetToken.GetAncestor<XmlNameAttributeSyntax>();
                if (xmlAttributeName != null)
                {
                    attributeName = xmlAttributeName.Name.LocalName.ValueText;
                }
                else
                {
                    attributeName = ((XmlNameSyntax)targetToken.GetAncestor<XmlAttributeSyntax>()!.Name).LocalName.ValueText;
                }

                items.AddRange(GetAttributeValueItems(symbol, tagName, attributeName));
            }
        }
    }

    protected override ImmutableArray<string> GetKeywordNames()
        => s_keywordNames;

    protected override IEnumerable<string?> GetExistingTopLevelElementNames(DocumentationCommentTriviaSyntax parentTrivia)
        => parentTrivia.Content
                       .Select(node => GetElementNameAndAttributes(node).Name)
                       .WhereNotNull();

    protected override IEnumerable<string> GetExistingTopLevelAttributeValues(DocumentationCommentTriviaSyntax syntax, string elementName, string attributeName)
    {
        var attributeValues = SpecializedCollections.EmptyEnumerable<string>();

        foreach (var node in syntax.Content)
        {
            var nameAndAttributes = GetElementNameAndAttributes(node);
            if (nameAndAttributes.Name == elementName)
            {
                attributeValues = attributeValues.Concat(
                    nameAndAttributes.Attributes
                                     .Where(attribute => GetAttributeName(attribute) == attributeName)
                                     .Select(GetAttributeValue));
            }
        }

        return attributeValues;
    }

    private static (string? Name, SyntaxList<XmlNodeSyntax> Attributes) GetElementNameAndAttributes(XmlNodeSyntax node)
    {
        XmlNameSyntax? nameSyntax = null;
        SyntaxList<XmlNodeSyntax> attributes = default;

        if (node.IsKind(SyntaxKind.XmlEmptyElement))
        {
            var emptyElementSyntax = (XmlEmptyElementSyntax)node;
            nameSyntax = emptyElementSyntax.Name as XmlNameSyntax;
            attributes = emptyElementSyntax.Attributes;
        }
        else if (node.IsKind(SyntaxKind.XmlElement))
        {
            var elementSyntax = (XmlElementSyntax)node;
            nameSyntax = elementSyntax.StartTag.Name as XmlNameSyntax;
            attributes = elementSyntax.StartTag.Attributes;
        }

        return (nameSyntax?.LocalName.ValueText, attributes);
    }

    private string GetAttributeValue(XmlNodeSyntax attribute)
    {
        if (attribute is XmlAttributeSyntax xmlAttribute)
        {
            // Decode any XML enities and concatentate the results
            return ((XmlStringSyntax)xmlAttribute.Value).TextTokens.GetValueText();
        }

        return (attribute as XmlNameAttributeSyntax)?.Reference?.Identifier.ValueText ?? string.Empty;
    }

    private IEnumerable<CompletionItem> GetAttributes(SyntaxToken token, string tagName, SyntaxList<XmlNodeSyntax> attributes)
    {
        var existingAttributeNames = attributes.Select(GetAttributeName).WhereNotNull().ToSet();
        var nextToken = token.GetNextToken();
        return GetAttributeItems(tagName, existingAttributeNames,
                                 addEqualsAndQuotes: !nextToken.IsKind(SyntaxKind.EqualsToken) || nextToken.HasLeadingTrivia);
    }

    private static string? GetAttributeName(XmlNodeSyntax node)
    {
        var nameSyntax = node.TypeSwitch(
            (XmlAttributeSyntax attribute) => attribute.Name as XmlNameSyntax,
            (XmlNameAttributeSyntax attribute) => attribute.Name,
            (XmlCrefAttributeSyntax attribute) => attribute.Name);

        return nameSyntax?.LocalName.ValueText;
    }

    protected override ImmutableArray<IParameterSymbol> GetParameters(ISymbol symbol)
    {
        var declaredParameters = symbol.GetParameters();
        var namedTypeSymbol = symbol as INamedTypeSymbol;
        if (namedTypeSymbol != null)
        {
            if (namedTypeSymbol.DelegateInvokeMethod != null)
            {
                declaredParameters = namedTypeSymbol.DelegateInvokeMethod.Parameters;
            }
        }

        return declaredParameters;
    }

    private static readonly CompletionItemRules s_defaultRules =
        CompletionItemRules.Create(
            filterCharacterRules: FilterRules,
            enterKeyRule: EnterKeyRule.Never);
}
