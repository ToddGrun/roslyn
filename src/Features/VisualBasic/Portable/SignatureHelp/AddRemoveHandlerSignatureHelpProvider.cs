// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("AddRemoveHandlerSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AddRemoveHandlerSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<AddRemoveHandlerStatementSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(AddRemoveHandlerStatementSyntax node, Document document, CancellationToken cancellationToken)
    {
        return node.Kind() switch
        {
            SyntaxKind.AddHandlerStatement => RoslynValueTaskExtensions.FromResult(SpecializedCollections.SingletonEnumerable<AbstractIntrinsicOperatorDocumentation>(new AddHandlerStatementDocumentation())),
            SyntaxKind.RemoveHandlerStatement => RoslynValueTaskExtensions.FromResult(SpecializedCollections.SingletonEnumerable<AbstractIntrinsicOperatorDocumentation>(new RemoveHandlerStatementDocumentation())),
            _ => RoslynValueTaskExtensions.FromResult(SpecializedCollections.EmptyEnumerable<AbstractIntrinsicOperatorDocumentation>())
        };
    }

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<AddRemoveHandlerStatementSyntax>(ce => ce.AddHandlerOrRemoveHandlerKeyword) ||
           token.IsChildToken<AddRemoveHandlerStatementSyntax>(ce => ce.CommaToken);

    public override ImmutableArray<char> TriggerCharacters => [' ', ','];

    public override ImmutableArray<char> RetriggerCharacters => [];

    protected override bool IsArgumentListToken(AddRemoveHandlerStatementSyntax node, SyntaxToken token)
        => true;
}
