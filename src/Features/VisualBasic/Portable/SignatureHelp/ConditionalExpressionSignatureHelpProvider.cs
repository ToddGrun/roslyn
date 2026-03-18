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

internal abstract class ConditionalExpressionSignatureHelpProvider<T> : AbstractIntrinsicOperatorSignatureHelpProvider<T>
    where T : SyntaxNode
{
    protected abstract SyntaxKind Kind { get; }

    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(T node, Document document, CancellationToken cancellationToken)
        => new([new BinaryConditionalExpressionDocumentation(), new TernaryConditionalExpressionDocumentation()]);

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) &&
           token.Parent.Kind() == Kind;

    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(T node, SyntaxToken token)
        => node.Span.Contains(token.SpanStart) &&
            (token.Kind() != SyntaxKind.CloseParenToken ||
            token.Parent.Kind() != Kind);
}

[ExportSignatureHelpProvider("BinaryConditionalExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BinaryConditionalExpressionSignatureHelpProvider() : ConditionalExpressionSignatureHelpProvider<BinaryConditionalExpressionSyntax>
{
    protected override SyntaxKind Kind => SyntaxKind.BinaryConditionalExpression;
}

[ExportSignatureHelpProvider("TernaryConditionalExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TernaryConditionalExpressionSignatureHelpProvider() : ConditionalExpressionSignatureHelpProvider<TernaryConditionalExpressionSyntax>
{
    protected override SyntaxKind Kind => SyntaxKind.TernaryConditionalExpression;
}
