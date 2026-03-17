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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(NamedParameterCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(EnumCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class NamedParameterCompletionProvider() : LSPCompletionProvider
{
    internal const string s_colonEquals = ":=";

    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerChars;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
                syntaxTree.IsInSkippedText(position, cancellationToken))
            {
                return;
            }

            var token = syntaxTree.GetTargetToken(position, cancellationToken);

            if (!token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
            {
                return;
            }

            var argumentList = token.Parent as ArgumentListSyntax;
            if (argumentList == null)
            {
                return;
            }

            if (token.Kind() == SyntaxKind.CommaToken)
            {
                // Consider refining this logic to mandate completion with an argument name, if preceded by an out-of-position name
                // See https://github.com/dotnet/roslyn/issues/20657
                var languageVersion = ((VisualBasicParseOptions)document.Project.ParseOptions).LanguageVersion;
                if (languageVersion < LanguageVersion.VisualBasic15_5 && token.IsMandatoryNamedParameterPosition())
                {
                    context.IsExclusive = true;
                }
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(argumentList, cancellationToken).ConfigureAwait(false);
            var parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent, cancellationToken);
            if (parameterLists == null)
            {
                return;
            }

            var existingNamedParameters = GetExistingNamedParameters(argumentList, position);
            parameterLists = parameterLists.Where(p => IsValid(p, existingNamedParameters));

            var unspecifiedParameters = parameterLists.SelectMany(pl => pl)
                                                       .Where(p => !existingNamedParameters.Contains(p.Name));

            var rightToken = syntaxTree.FindTokenOnRightOfPosition(position, cancellationToken);
            var textSuffix = rightToken.IsKind(SyntaxKind.ColonEqualsToken) ? null : s_colonEquals;

            foreach (var parameter in unspecifiedParameters)
            {
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: parameter.Name,
                    displayTextSuffix: textSuffix,
                    insertionText: parameter.Name.ToIdentifierToken().ToString() + textSuffix,
                    symbols: ImmutableArray.Create(parameter),
                    contextPosition: position,
                    rules: s_itemRules));
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
        {
            // nop
        }
    }

    // Typing : or = should not filter the list, but they should commit the list.
    private static readonly CompletionItemRules s_itemRules = CompletionItemRules.Default
        .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':', '='))
        .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, ':', '='));

    internal override Task<CompletionDescription?> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        return SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
    }

    private static bool IsValid(ImmutableArray<ISymbol> parameterList, ISet<string> existingNamedParameters)
    {
        // A parameter list is valid if it has parameters that match in name all the existing
        // named parameters that have been provided.
        return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
    }

    private static ISet<string> GetExistingNamedParameters(ArgumentListSyntax argumentList, int position)
    {
        var existingArguments =
            argumentList.Arguments.OfType<SimpleArgumentSyntax>()
                                   .Where(n => n.IsNamed && !n.NameColonEquals.ColonEqualsToken.IsMissing && n.NameColonEquals.Span.End <= position)
                                   .Select(a => a.NameColonEquals.Name.Identifier.ValueText)
                                   .Where(i => !string.IsNullOrWhiteSpace(i));

        return existingArguments.ToSet();
    }

    private static IEnumerable<ImmutableArray<ISymbol>>? GetParameterLists(SemanticModel semanticModel,
                                       int position,
                                       SyntaxNode invocableNode,
                                       CancellationToken cancellationToken)
    {
        return invocableNode.TypeSwitch(
            (AttributeSyntax attribute) => GetAttributeParameterLists(semanticModel, position, attribute, cancellationToken),
            (InvocationExpressionSyntax invocationExpression) => GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken),
            (ObjectCreationExpressionSyntax objectCreationExpression) => GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken));
    }

    private static IEnumerable<ImmutableArray<ISymbol>>? GetObjectCreationExpressionParameterLists(SemanticModel semanticModel,
                                                               int position,
                                                               ObjectCreationExpressionSyntax objectCreationExpression,
                                                               CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);

        if (type != null && within != null && type.TypeKind != TypeKind.Delegate)
        {
            return type.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
                                             .Select(c => c.Parameters.As<ISymbol>());
        }

        return null;
    }

    private static IEnumerable<ImmutableArray<ISymbol>>? GetAttributeParameterLists(SemanticModel semanticModel,
                                                int position,
                                                AttributeSyntax attribute,
                                                CancellationToken cancellationToken)
    {
        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;

        var namedParameters = attributeType.GetAttributeNamedParameters(semanticModel.Compilation, within);
        return SpecializedCollections.SingletonEnumerable(
            ImmutableArray.CreateRange(namedParameters));
    }

    private static IEnumerable<ImmutableArray<ISymbol>>? GetInvocationExpressionParameterLists(SemanticModel semanticModel,
                                                           int position,
                                                           InvocationExpressionSyntax invocationExpression,
                                                           CancellationToken cancellationToken)
    {
        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        var expression = invocationExpression.GetExpression();
        if (within != null && expression != null)
        {
            var memberGroup = semanticModel.GetMemberGroup(expression, cancellationToken);
            var expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            var indexers = expressionType == null
                               ? SpecializedCollections.EmptyList<IPropertySymbol>()
                               : semanticModel.LookupSymbols(position, expressionType, includeReducedExtensionMethods: true).OfType<IPropertySymbol>().Where(p => p.IsIndexer).ToList();

            if (memberGroup.Length > 0)
            {
                var accessibleMembers = memberGroup.Where(m => m.IsAccessibleWithin(within));
                var methodParameters = accessibleMembers.OfType<IMethodSymbol>().Select(m => m.Parameters.As<ISymbol>());
                var propertyParameters = accessibleMembers.OfType<IPropertySymbol>().Select(p => p.Parameters.As<ISymbol>());
                return methodParameters.Concat(propertyParameters);
            }
            else if (expressionType.IsDelegateType())
            {
                var delegateType = (INamedTypeSymbol)expressionType;
                return SpecializedCollections.SingletonEnumerable(delegateType.DelegateInvokeMethod.Parameters.As<ISymbol>());
            }
            else if (indexers.Count > 0)
            {
                return indexers.Where(i => i.IsAccessibleWithin(within, throughType: expressionType))
                                .Select(i => i.Parameters.As<ISymbol>());
            }
        }

        return null;
    }

    protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        var symbolItem = selectedItem;
        var insertionText = SymbolCompletionItem.GetInsertionText(selectedItem);
        TextChange change;
        if (ch.HasValue && ch.Value == ':')
        {
            change = new TextChange(symbolItem.Span, insertionText.Substring(0, insertionText.Length - s_colonEquals.Length));
        }
        else if (ch.HasValue && ch.Value == '=')
        {
            change = new TextChange(selectedItem.Span, insertionText.Substring(0, insertionText.Length - (s_colonEquals.Length - 1)));
        }
        else
        {
            change = new TextChange(symbolItem.Span, insertionText);
        }

        return Task.FromResult<TextChange?>(change);
    }
}
