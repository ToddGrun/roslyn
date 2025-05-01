// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.CodeAnalysis.Options;

internal static class TextBufferOptionProviders
{
    public static DocumentationCommentOptions GetDocumentationCommentOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, LanguageServices languageServices)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat: false);
        return optionsProvider.GlobalOptions.GetDocumentationCommentOptions(lineFormattingOptions, languageServices.Language);
    }

    public static LineFormattingOptions GetLineFormattingOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, bool explicitFormat)
       => GetLineFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), optionsProvider.IndentationManager, explicitFormat);

    private static LineFormattingOptions GetLineFormattingOptionsImpl(ITextBuffer textBuffer, IEditorOptions editorOptions, IIndentationManagerService indentationManager, bool explicitFormat)
    {
        indentationManager.GetIndentation(textBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

        return new LineFormattingOptions()
        {
            UseTabs = !convertTabsToSpaces,
            IndentationSize = indentSize,
            TabSize = tabSize,
            NewLine = editorOptions.GetNewLineCharacter(),
        };
    }

    private static LineFormattingOptions GetLineFormattingOptionsImpl(ITextSnapshotLine snapshotLine, IEditorOptions editorOptions, IIndentationManagerService indentationManager, bool explicitFormat)
    {
        indentationManager.GetIndentation(snapshotLine.Snapshot.TextBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

        return new LineFormattingOptions()
        {
            UseTabs = !convertTabsToSpaces,
            IndentationSize = indentSize,
            TabSize = tabSize,
            NewLine = GetNewLineCharacterToInsert(snapshotLine, editorOptions),
        };
    }

    public static string GetNewLineCharacterToInsert(ITextSnapshotLine line, IEditorOptions editorOptions)
    {
        string? lineBreak = null;
        var snapshot = line.Snapshot;

        if (editorOptions.GetReplicateNewLineCharacter())
        {
            if (line.LineBreakLength > 0)
            {
                // use the same line ending as the current line
                lineBreak = line.GetLineBreakText();
            }
            else
            {
                if (snapshot.LineCount > 1)
                {
                    // use the same line ending as the penultimate line in the buffer
                    lineBreak = snapshot.GetLineFromLineNumber(snapshot.LineCount - 2).GetLineBreakText();
                }
            }
        }

        var textToInsert = lineBreak ?? editorOptions.GetNewLineCharacter();
        return textToInsert;
    }

    public static async Task<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, ITextBuffer textBuffer, TextSpan? textSpan, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textSnapshot = Microsoft.CodeAnalysis.Text.Extensions.FindCorrespondingEditorTextSnapshot(text);

        if (textSnapshot != null)
        {
            var snapshotPoint = textSnapshot.GetPoint(textSpan?.Start ?? 0);
            return GetSyntaxFormattingOptions(snapshotPoint, optionsProvider, fallbackOptions, languageServices, explicitFormat);
        }

        return GetSyntaxFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);
    }

    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this SnapshotPoint snapshotPoint, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
    {
        return GetSyntaxFormattingOptionsImpl(snapshotPoint.GetContainingLine(), optionsProvider.Factory.GetOptions(snapshotPoint.Snapshot.TextBuffer), fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);
    }

    private static SyntaxFormattingOptions GetSyntaxFormattingOptionsImpl(ITextBuffer textBuffer, IEditorOptions editorOptions, StructuredAnalyzerConfigOptions fallbackOptions, IIndentationManagerService indentationManager, LanguageServices languageServices, bool explicitFormat)
    {
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        var options = configOptions.GetSyntaxFormattingOptions(languageServices);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, indentationManager, explicitFormat);

        return options with { LineFormatting = lineFormattingOptions };
    }

    private static SyntaxFormattingOptions GetSyntaxFormattingOptionsImpl(ITextSnapshotLine snapshotLine, IEditorOptions editorOptions, StructuredAnalyzerConfigOptions fallbackOptions, IIndentationManagerService indentationManager, LanguageServices languageServices, bool explicitFormat)
    {
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        var options = configOptions.GetSyntaxFormattingOptions(languageServices);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(snapshotLine, editorOptions, indentationManager, explicitFormat);

        return options with { LineFormatting = lineFormattingOptions };
    }

    public static IndentationOptions GetIndentationOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var formattingOptions = GetSyntaxFormattingOptionsImpl(textBuffer, editorOptions, fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);

        return new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = optionsProvider.GlobalOptions.GetAutoFormattingOptions(languageServices.Language),
            // TODO: Call editorOptions.GetIndentStyle() instead (see https://github.com/dotnet/roslyn/issues/62204):
            IndentStyle = optionsProvider.GlobalOptions.GetOption(IndentationOptionsStorage.SmartIndent, languageServices.Language)
        };
    }

    public static AddImportPlacementOptions GetAddImportPlacementOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool allowInHiddenRegions)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        return configOptions.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions);
    }

    public static CodeCleanupOptions GetCodeCleanupOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat, bool allowImportsInHiddenRegions)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);

        var options = configOptions.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat);

        return options with { FormattingOptions = options.FormattingOptions with { LineFormatting = lineFormattingOptions } };
    }

    public static IndentingStyle ToEditorIndentStyle(this FormattingOptions2.IndentStyle value)
        => value switch
        {
            FormattingOptions2.IndentStyle.Smart => IndentingStyle.Smart,
            FormattingOptions2.IndentStyle.Block => IndentingStyle.Block,
            _ => IndentingStyle.None,
        };

    public static FormattingOptions2.IndentStyle ToIndentStyle(this IndentingStyle value)
        => value switch
        {
            IndentingStyle.Smart => FormattingOptions2.IndentStyle.Smart,
            IndentingStyle.Block => FormattingOptions2.IndentStyle.Block,
            _ => FormattingOptions2.IndentStyle.None,
        };
}
