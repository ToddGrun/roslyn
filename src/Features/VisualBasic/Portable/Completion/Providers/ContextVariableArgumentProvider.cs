// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportArgumentProvider(nameof(ContextVariableArgumentProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(FirstBuiltInArgumentProvider))]
[Shared]
internal class ContextVariableArgumentProvider : AbstractContextVariableArgumentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ContextVariableArgumentProvider()
    {
    }

    protected override string ThisOrMeKeyword => SyntaxFacts.GetText(SyntaxKind.MeKeyword);

    protected override bool IsInstanceContext(SyntaxTree syntaxTree, SyntaxToken targetToken, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // Uses logic from MeKeywordRecommender, restricted by knowledge that this is an invocation within an
        // expression
        if (targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock))
        {
            if (targetToken.GetContainingMemberBlockBegin().TypeSwitch(
                (MethodBaseSyntax methodBase) => !methodBase.Modifiers.Any(SyntaxKind.SharedKeyword),
                (PropertyStatementSyntax propertyStatement) => !propertyStatement.Modifiers.Any(SyntaxKind.SharedKeyword),
                (EventStatementSyntax eventStatement) => !eventStatement.Modifiers.Any(SyntaxKind.SharedKeyword)))
            {
                return true;
            }

            var containingMember = targetToken.GetContainingMember();
            if (containingMember is FieldDeclarationSyntax fieldDecl)
            {
                if (!fieldDecl.Modifiers.Any(SyntaxKind.SharedKeyword))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
