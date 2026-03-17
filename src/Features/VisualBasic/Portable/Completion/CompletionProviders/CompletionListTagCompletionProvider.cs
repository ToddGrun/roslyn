// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(CompletionListTagCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(CrefCompletionProvider))]
[Shared]
internal class CompletionListTagCompletionProvider : EnumCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CompletionListTagCompletionProvider()
    {
    }

    protected override Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext? completionContext,
        VisualBasicSyntaxContext syntaxContext,
        int position,
        CompletionOptions options,
        CancellationToken cancellationToken)
    {
        if (syntaxContext.SyntaxTree.IsObjectCreationTypeContext(position, cancellationToken) ||
            syntaxContext.SyntaxTree.IsInNonUserCode(position, cancellationToken))
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
        }

        if (syntaxContext.TargetToken.IsKind(SyntaxKind.DotToken))
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
        }

        var typeInferenceService = syntaxContext.GetLanguageService<ITypeInferenceService>();
        var inferredType = typeInferenceService.InferType(syntaxContext.SemanticModel, position, objectAsDefault: true, cancellationToken: cancellationToken);
        if (inferredType == null)
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
        }

        var within = syntaxContext.SemanticModel.GetEnclosingNamedType(position, cancellationToken);
        var completionListType = GetCompletionListType(inferredType, within, syntaxContext.SemanticModel.Compilation, cancellationToken);

        if (completionListType == null)
        {
            return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
        }

        var builder = ArrayBuilder<SymbolAndSelectionInfo>.GetInstance();
        foreach (var member in completionListType.GetAccessibleMembersInThisAndBaseTypes<ISymbol>(within))
        {
            if (member.MatchesKind(SymbolKind.Field, SymbolKind.Property) &&
                member.IsStatic &&
                member.IsAccessibleWithin(within) &&
                member.IsEditorBrowsable(options.MemberDisplayOptions.HideAdvancedMembers, syntaxContext.SemanticModel.Compilation))
            {
                builder.Add(new SymbolAndSelectionInfo(member, Preselect: true));
            }
        }

        return Task.FromResult(builder.ToImmutableAndFree());
    }

    private static ITypeSymbol? GetCompletionListType(ITypeSymbol inferredType, INamedTypeSymbol within, Compilation compilation, CancellationToken cancellationToken)
    {
        var documentation = inferredType.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken);
        if (documentation.CompletionListCref != null)
        {
            var crefType = DocumentationCommentId.GetSymbolsForDeclarationId(documentation.CompletionListCref, compilation)
                                .OfType<INamedTypeSymbol>()
                                .FirstOrDefault();

            if (crefType != null && crefType.IsAccessibleWithin(within))
            {
                return crefType;
            }
        }

        return null;
    }

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, VisualBasicSyntaxContext context)
    {
        var displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithKindOptions(SymbolDisplayKindOptions.None);
        var text = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat);
        return (text, "", text);
    }

    protected override CompletionItem CreateItem(
        CompletionContext? completionContext,
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        VisualBasicSyntaxContext context,
        SupportedPlatformData? supportedPlatformData)
    {
        // Use symbol name (w/o containing type) as additional filter text, which would
        // promote this item during matching when user types member name only, like "Empty"
        // instead of "ImmutableArray.Empty"
        var additionalFilterTexts = ImmutableArray.Create(symbols[0].Symbol.Name);
        return SymbolCompletionItem.CreateWithSymbolId(
            displayText: displayText,
            displayTextSuffix: displayTextSuffix,
            insertionText: insertionText,
            filterText: displayText,
            symbols: symbols.SelectAsArray(t => t.Symbol),
            rules: CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect),
            contextPosition: context.Position,
            sortText: displayText,
            supportedPlatforms: supportedPlatformData,
            tags: WellKnownTagArrays.TargetTypeMatch).WithAdditionalFilterTexts(additionalFilterTexts);
    }
}
