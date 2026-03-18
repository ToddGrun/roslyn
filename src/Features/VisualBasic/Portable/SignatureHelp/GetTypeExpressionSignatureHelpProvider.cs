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

[ExportSignatureHelpProvider("GetTypeExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class GetTypeExpressionSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<GetTypeExpressionSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(GetTypeExpressionSyntax node, Document document, CancellationToken cancellationToken)
        => new([new GetTypeExpressionDocumentation()]);

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<GetTypeExpressionSyntax>(ce => ce.OpenParenToken);

    public override ImmutableArray<char> TriggerCharacters => ['('];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(GetTypeExpressionSyntax node, SyntaxToken token)
        => node.GetTypeKeyword != token &&
            node.CloseParenToken != token;
}
