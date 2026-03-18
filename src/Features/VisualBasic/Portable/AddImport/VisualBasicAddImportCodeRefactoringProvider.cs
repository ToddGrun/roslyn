// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.LanguageService;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.AddImport;

[ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.AddImport), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualBasicAddImportCodeRefactoringProvider()
    : AbstractAddImportCodeRefactoringProvider<
        ExpressionSyntax,
        MemberAccessExpressionSyntax,
        NameSyntax,
        SimpleNameSyntax,
        QualifiedNameSyntax,
        GlobalNameSyntax,
        ImportsStatementSyntax>(VisualBasicSyntaxFacts.Instance)
{
    protected override string AddImportTitle => VBFeaturesResources.Add_Imports_0;
    protected override string AddImportAndSimplifyAllOccurrencesTitle => VBFeaturesResources.Add_Imports_0_and_simplify_all_occurrences;
}
