// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp;

[ExportSignatureHelpProvider("PredefinedCastExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class PredefinedCastExpressionSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<PredefinedCastExpressionSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(PredefinedCastExpressionSyntax node, Document document, CancellationToken cancellationToken)
        => new(GetIntrinsicOperatorDocumentationImplAsync(node, document, cancellationToken));

    private static async Task<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationImplAsync(PredefinedCastExpressionSyntax node, Document document, CancellationToken cancellationToken)
        => SpecializedCollections.SingletonEnumerable(new PredefinedCastExpressionDocumentation(node.Keyword.Kind(), await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)));

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<PredefinedCastExpressionSyntax>(ce => ce.OpenParenToken);

    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(PredefinedCastExpressionSyntax node, SyntaxToken token)
        => node.Keyword != token &&
            node.CloseParenToken != token;
}
