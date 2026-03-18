// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic.Indentation;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Wrapping.ChainedExpression;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.ChainedExpression;

internal class VisualBasicChainedExpressionWrapper : AbstractChainedExpressionWrapper<NameSyntax, ArgumentListSyntax>
{
    public VisualBasicChainedExpressionWrapper()
        : base(VisualBasicIndentationService.WithoutParameterAlignmentInstance, VisualBasicSyntaxFacts.Instance)
    {
    }

    protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
    {
        return newLine.InsertRange(0, new[] { SyntaxFactory.WhitespaceTrivia(" "), SyntaxFactory.LineContinuationTrivia("_") });
    }
}
