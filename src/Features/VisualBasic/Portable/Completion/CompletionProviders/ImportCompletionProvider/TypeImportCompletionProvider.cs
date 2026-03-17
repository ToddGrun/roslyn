// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(TypeImportCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(AggregateEmbeddedLanguageCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TypeImportCompletionProvider() : AbstractTypeImportCompletionProvider<SimpleImportsClauseSyntax>
{
    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return IsDefaultTriggerCharacterOrParen(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters => CommonTriggerCharsAndParen;

    protected override bool IsFinalSemicolonOfUsingOrExtern(SyntaxNode directive, SyntaxToken token)
    {
        return false;
    }

    protected override Task<bool> ShouldProvideParenthesisCompletionAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
