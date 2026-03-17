// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(InternalsVisibleToCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(XmlDocCommentCompletionProvider))]
[Shared]
internal sealed class InternalsVisibleToCompletionProvider : AbstractInternalsVisibleToCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InternalsVisibleToCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    protected override IImmutableList<SyntaxNode> GetAssemblyScopedAttributeSyntaxNodesOfDocument(SyntaxNode documentRoot)
    {
        ImmutableList<SyntaxNode>.Builder? builder = null;
        var compilationUnit = documentRoot as CompilationUnitSyntax;
        if (compilationUnit != null)
        {
            foreach (var attributeStatement in compilationUnit.Attributes)
            {
                foreach (var attributeList in attributeStatement.AttributeLists)
                {
                    builder ??= ImmutableList.CreateBuilder<SyntaxNode>();
                    builder.AddRange(attributeList.Attributes);
                }
            }
        }

        return builder == null ? ImmutableList<SyntaxNode>.Empty : builder.ToImmutable();
    }

    protected override SyntaxNode? GetConstructorArgumentOfInternalsVisibleToAttribute(SyntaxNode internalsVisibleToAttribute)
    {
        var arguments = ((AttributeSyntax)internalsVisibleToAttribute).ArgumentList.Arguments;
        // InternalsVisibleTo has only one constructor argument.
        // https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.internalsvisibletoattribute(v=vs.110).aspx
        // We can assume that this is the assemblyName argument.
        return arguments.Count > 0 ? arguments[0].GetExpression() : null;
    }

    protected override bool ShouldTriggerAfterQuotes(SourceText text, int insertedCharacterPosition)
        => CompletionUtilities.IsWordStartCharacter(text[insertedCharacterPosition]);
}
