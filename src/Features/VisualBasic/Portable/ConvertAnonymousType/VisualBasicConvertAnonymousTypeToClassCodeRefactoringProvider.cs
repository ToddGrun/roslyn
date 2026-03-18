// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertAnonymousType;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousType;

[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
[ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class VisualBasicConvertAnonymousTypeToClassCodeRefactoringProvider()
    : AbstractConvertAnonymousTypeToClassCodeRefactoringProvider<
        ExpressionSyntax,
        NameSyntax,
        IdentifierNameSyntax,
        ObjectCreationExpressionSyntax,
        AnonymousObjectCreationExpressionSyntax,
        NamespaceBlockSyntax>
{
    protected override ObjectCreationExpressionSyntax CreateObjectCreationExpression(
        NameSyntax nameNode, AnonymousObjectCreationExpressionSyntax anonymousObject)
    {
        return SyntaxFactory.ObjectCreationExpression(
            attributeLists: default, nameNode, CreateArgumentList(anonymousObject.Initializer), initializer: null);
    }

    private ArgumentListSyntax CreateArgumentList(ObjectMemberInitializerSyntax initializer)
    {
        return SyntaxFactory.ArgumentList(
            SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTriviaFrom(initializer.OpenBraceToken),
            CreateArguments(initializer.Initializers),
            SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(initializer.CloseBraceToken));
    }

    private SeparatedSyntaxList<ArgumentSyntax> CreateArguments(SeparatedSyntaxList<FieldInitializerSyntax> initializers)
    {
        return SyntaxFactory.SeparatedList<ArgumentSyntax>(CreateArguments(initializers.GetWithSeparators()));
    }

    private SyntaxNodeOrTokenList CreateArguments(SyntaxNodeOrTokenList list)
    {
        return new SyntaxNodeOrTokenList(list.Select(CreateArgumentOrComma));
    }

    private SyntaxNodeOrToken CreateArgumentOrComma(SyntaxNodeOrToken declOrComma)
    {
        return declOrComma.IsToken
            ? declOrComma
            : CreateArgument((FieldInitializerSyntax)declOrComma);
    }

    private static ArgumentSyntax CreateArgument(FieldInitializerSyntax initializer)
    {
        var expression = (initializer as InferredFieldInitializerSyntax)?.Expression
                      ?? (initializer as NamedFieldInitializerSyntax)?.Expression;
        return SyntaxFactory.SimpleArgument(expression);
    }
}
