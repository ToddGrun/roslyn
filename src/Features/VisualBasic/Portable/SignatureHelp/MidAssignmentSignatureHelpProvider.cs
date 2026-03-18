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

[ExportSignatureHelpProvider("MidAssignmentSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class MidAssignmentSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<AssignmentStatementSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(AssignmentStatementSyntax node, Document document, CancellationToken cancellationToken)
        => new([new MidAssignmentDocumentation()]);

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) &&
           token.Parent.Kind() == SyntaxKind.ArgumentList &&
           token.Parent.IsParentKind(SyntaxKind.MidExpression) &&
           token.Parent.Parent.IsParentKind(SyntaxKind.MidAssignmentStatement);

    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(AssignmentStatementSyntax node, SyntaxToken token)
        => node.Left.IsKind(SyntaxKind.MidExpression) &&
            ((MidExpressionSyntax)node.Left).ArgumentList.Span.Contains(token.SpanStart) &&
            ((MidExpressionSyntax)node.Left).ArgumentList.CloseParenToken != token;

    protected override SignatureHelpState? GetCurrentArgumentStateWorker(SyntaxNode node, int position)
        => base.GetCurrentArgumentStateWorker(((MidExpressionSyntax)((AssignmentStatementSyntax)node).Left).ArgumentList, position);
}
