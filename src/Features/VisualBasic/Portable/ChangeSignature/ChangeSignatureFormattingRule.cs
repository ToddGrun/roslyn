// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;

namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature;

internal sealed class ChangeSignatureFormattingRule : BaseFormattingRule
{
    public override void AddIndentBlockOperationsSlow(List<IndentBlockOperation> list, SyntaxNode node, ref NextIndentBlockOperationAction nextOperation)
    {
        nextOperation.Invoke();

        if (node.IsKind(SyntaxKind.ParameterList) || node.IsKind(SyntaxKind.ArgumentList))
        {
            AddChangeSignatureIndentOperation(list, node);
        }
    }

    private static void AddChangeSignatureIndentOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node.Parent != null)
        {
            var firstToken = node.GetFirstToken();
            var lastToken = node.GetLastToken();
            list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                node.Parent.GetFirstToken(),
                firstToken,
                lastToken,
                new TextSpan(firstToken.SpanStart, lastToken.Span.End - firstToken.SpanStart),
                indentationDelta: 1,
                option: IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
        }
    }

    public override AdjustNewLinesOperation GetAdjustNewLinesOperationSlow(ref SyntaxToken previousToken, ref SyntaxToken currentToken, ref NextGetAdjustNewLinesOperation nextOperation)
    {
        if (previousToken.IsKind(SyntaxKind.CommaToken) &&
           (previousToken.Parent.IsKind(SyntaxKind.ParameterList) || previousToken.Parent.IsKind(SyntaxKind.ArgumentList)))
        {
            return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
        }

        return base.GetAdjustNewLinesOperationSlow(ref previousToken, ref currentToken, ref nextOperation);
    }
}
