// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.UseNamedArguments;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.UseNamedArguments;

[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
[ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.UseNamedArguments), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class VisualBasicUseNamedArgumentsCodeRefactoringProvider()
    : AbstractUseNamedArgumentsCodeRefactoringProvider(new ArgumentAnalyzer(), attributeArgumentAnalyzer: null)
{
    private class ArgumentAnalyzer : Analyzer<ArgumentSyntax, SimpleArgumentSyntax, ArgumentListSyntax>
    {
        protected override bool IsPositionalArgument(SimpleArgumentSyntax argument)
        {
            return argument.NameColonEquals is null;
        }

        protected override SeparatedSyntaxList<ArgumentSyntax> GetArguments(ArgumentListSyntax argumentList)
        {
            return argumentList.Arguments;
        }

        protected override SyntaxNode GetReceiver(SyntaxNode argument)
        {
            if (argument.Parent.IsParentKind(SyntaxKind.Attribute))
            {
                return null;
            }

            return argument.Parent.Parent;
        }

        protected override SimpleArgumentSyntax WithName(SimpleArgumentSyntax argument, string name)
        {
            return argument.WithNameColonEquals(SyntaxFactory.NameColonEquals(name.ToIdentifierName()));
        }

        protected override ArgumentListSyntax WithArguments(ArgumentListSyntax argumentList, IEnumerable<ArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators)
        {
            return argumentList.WithArguments(SyntaxFactory.SeparatedList(namedArguments, separators));
        }

        protected override bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount)
        {
            return !parameters.LastOrDefault().IsParams || parameters.Length > argumentCount;
        }

        protected override bool SupportsNonTrailingNamedArguments(ParseOptions options)
        {
            return ((VisualBasicParseOptions)options).LanguageVersion >= LanguageVersion.VisualBasic15_5;
        }

        protected override bool IsImplicitIndexOrRangeIndexer(ImmutableArray<IParameterSymbol> parameters, ArgumentSyntax argument, SemanticModel semanticModel)
        {
            return false;
        }
    }
}
