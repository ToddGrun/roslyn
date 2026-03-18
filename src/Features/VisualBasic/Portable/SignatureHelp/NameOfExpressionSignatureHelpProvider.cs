// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider(nameof(NameOfExpressionSignatureHelpProvider), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class NameOfExpressionSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<NameOfExpressionSyntax>
{
    public override ImmutableArray<char> TriggerCharacters => ['('];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(NameOfExpressionSyntax node, Document document, CancellationToken cancellationToken)
        => new([new NameOfExpressionDocumentation()]);

    protected override bool IsArgumentListToken(NameOfExpressionSyntax node, SyntaxToken token)
        => node.NameOfKeyword != token &&
            node.CloseParenToken != token;

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<NameOfExpressionSyntax>(noe => noe.OpenParenToken);
}
