// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic.Indentation;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Precedence;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Wrapping.BinaryExpression;

namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.BinaryExpression;

internal class VisualBasicBinaryExpressionWrapper : AbstractBinaryExpressionWrapper<BinaryExpressionSyntax>
{
    public VisualBasicBinaryExpressionWrapper()
        : base(VisualBasicIndentationService.WithoutParameterAlignmentInstance,
               VisualBasicSyntaxFacts.Instance,
               VisualBasicPrecedenceService.Instance)
    {
        // Override default indentation behavior.  The special indentation rule tries to
        // align parameters.  But that's what we're actually trying to control, so we need
        // to remove this.
    }

    protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
    {
        return newLine.InsertRange(0, new[] { SyntaxFactory.WhitespaceTrivia(" "), SyntaxFactory.LineContinuationTrivia("_") });
    }
}
