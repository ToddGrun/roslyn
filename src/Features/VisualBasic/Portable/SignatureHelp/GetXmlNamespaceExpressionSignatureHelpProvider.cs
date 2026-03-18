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

[ExportSignatureHelpProvider("GetXmlNamespaceExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class GetXmlNamespaceExpressionSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<GetXmlNamespaceExpressionSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(GetXmlNamespaceExpressionSyntax node, Document document, CancellationToken cancellationToken)
        => new([new GetXmlNamespaceExpressionDocumentation()]);

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<GetXmlNamespaceExpressionSyntax>(ce => ce.OpenParenToken);

    public override ImmutableArray<char> TriggerCharacters => ['('];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(GetXmlNamespaceExpressionSyntax node, SyntaxToken token)
        => node.GetXmlNamespaceKeyword != token &&
            node.CloseParenToken != token;
}
