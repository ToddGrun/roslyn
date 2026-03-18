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

[ExportSignatureHelpProvider("CastExpressionSignatureHelpProvider", LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CastExpressionSignatureHelpProvider() : AbstractIntrinsicOperatorSignatureHelpProvider<CastExpressionSyntax>
{
    protected override ValueTask<IEnumerable<AbstractIntrinsicOperatorDocumentation>> GetIntrinsicOperatorDocumentationAsync(CastExpressionSyntax node, Document document, CancellationToken cancellationToken)
    {
        return node.Kind() switch
        {
            SyntaxKind.CTypeExpression => RoslynValueTaskExtensions.FromResult<IEnumerable<AbstractIntrinsicOperatorDocumentation>>([new CTypeCastExpressionDocumentation()]),
            SyntaxKind.DirectCastExpression => RoslynValueTaskExtensions.FromResult<IEnumerable<AbstractIntrinsicOperatorDocumentation>>([new DirectCastExpressionDocumentation()]),
            SyntaxKind.TryCastExpression => RoslynValueTaskExtensions.FromResult<IEnumerable<AbstractIntrinsicOperatorDocumentation>>([new TryCastExpressionDocumentation()]),
            _ => RoslynValueTaskExtensions.FromResult(SpecializedCollections.EmptyEnumerable<AbstractIntrinsicOperatorDocumentation>())
        };
    }

    protected override bool IsTriggerToken(SyntaxToken token)
        => token.IsChildToken<CastExpressionSyntax>(ce => ce.OpenParenToken) ||
           token.IsChildToken<CastExpressionSyntax>(ce => ce.CommaToken);

    public override ImmutableArray<char> TriggerCharacters => ['(', ','];

    public override ImmutableArray<char> RetriggerCharacters => [')'];

    protected override bool IsArgumentListToken(CastExpressionSyntax node, SyntaxToken token)
        => node.Span.Contains(token.SpanStart) &&
            node.OpenParenToken.SpanStart <= token.SpanStart &&
            token != node.CloseParenToken;
}
