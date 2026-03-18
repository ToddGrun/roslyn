// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.AddImport;

[ExportLanguageService(typeof(IAddImportFeatureService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualBasicAddImportFeatureService()
    : AbstractAddImportFeatureService<SimpleNameSyntax>
{
    protected override bool IsWithinImport(SyntaxNode node)
    {
        return node.GetAncestor<ImportsStatementSyntax>() != null;
    }

    protected override bool CanAddImport(SyntaxNode node, bool allowInHiddenRegions, CancellationToken cancellationToken)
    {
        return node.CanAddImportsStatements(allowInHiddenRegions, cancellationToken);
    }

    protected override bool CanAddImportForMember(
        string diagnosticId,
        ISyntaxFacts syntaxFacts,
        SyntaxNode node,
        out SimpleNameSyntax nameNode)
    {
        nameNode = null;

        switch (diagnosticId)
        {
            case AddImportDiagnosticIds.BC30456:
            case AddImportDiagnosticIds.BC30390:
            case AddImportDiagnosticIds.BC42309:
            case AddImportDiagnosticIds.BC30451:
                break;
            case AddImportDiagnosticIds.BC30512:
                // look up its corresponding method name
                var parent = node.GetAncestor<InvocationExpressionSyntax>();
                if (parent == null)
                {
                    return false;
                }

                var method = parent.Expression as MemberAccessExpressionSyntax;
                if (method != null)
                {
                    node = method.Name;
                }
                else
                {
                    node = parent.Expression;
                }

                break;
            case AddImportDiagnosticIds.BC36719:
                if (node.IsKind(SyntaxKind.ObjectCollectionInitializer))
                {
                    return true;
                }

                return false;
            case AddImportDiagnosticIds.BC32016:
                var memberAccessName = (node as MemberAccessExpressionSyntax)?.Name;
                var conditionalAccessName = (((node as ConditionalAccessExpressionSyntax)?.WhenNotNull as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax)?.Name;

                if (memberAccessName == null && conditionalAccessName == null)
                {
                    return false;
                }

                node = memberAccessName ?? conditionalAccessName;
                break;
            default:
                return false;
        }

        var memberAccess = node as MemberAccessExpressionSyntax;
        if (memberAccess != null)
        {
            node = memberAccess.Name;
        }

        if (memberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            return false;
        }

        nameNode = node as SimpleNameSyntax;
        if (nameNode == null)
        {
            return false;
        }

        return true;
    }

    protected override bool CanAddImportForNamespace(string diagnosticId, SyntaxNode node, out SimpleNameSyntax nameNode)
    {
        nameNode = null;

        switch (diagnosticId)
        {
            case AddImportDiagnosticIds.BC30002:
            case IDEDiagnosticIds.UnboundIdentifierId:
            case AddImportDiagnosticIds.BC30451:
                break;
            default:
                return false;
        }

        return CanAddImportForTypeOrNamespaceCore(node, out nameNode);
    }

    protected override bool CanAddImportForDeconstruct(string diagnosticId, SyntaxNode node)
    {
        // Not supported yet.
        return false;
    }

    protected override bool CanAddImportForGetAwaiter(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
    {
        return diagnosticId == AddImportDiagnosticIds.BC36930 &&
            AncestorOrSelfIsAwaitExpression(syntaxFactsService, node);
    }

    protected override bool CanAddImportForGetEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
    {
        return false;
    }

    protected override bool CanAddImportForGetAsyncEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
    {
        return false;
    }

    protected override bool CanAddImportForQuery(string diagnosticId, SyntaxNode node)
    {
        return diagnosticId == AddImportDiagnosticIds.BC36593 &&
            node.GetAncestor<QueryExpressionSyntax>() != null;
    }

    protected override bool CanAddImportForTypeOrNamespace(
        string diagnosticId, SyntaxNode node, out SimpleNameSyntax nameNode)
    {
        nameNode = null;

        switch (diagnosticId)
        {
            case AddImportDiagnosticIds.BC30002:
            case IDEDiagnosticIds.UnboundIdentifierId:
            case AddImportDiagnosticIds.BC30451:
            case AddImportDiagnosticIds.BC32042:
            case AddImportDiagnosticIds.BC32045:
            case AddImportDiagnosticIds.BC30389:
            case AddImportDiagnosticIds.BC31504:
            case AddImportDiagnosticIds.BC36610:
            case AddImportDiagnosticIds.BC30182:
                break;
            case AddImportDiagnosticIds.BC42309:
                switch (node.Kind())
                {
                    case SyntaxKind.XmlCrefAttribute:
                        node = ((XmlCrefAttributeSyntax)node).Reference.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                        break;
                    case SyntaxKind.CrefReference:
                        node = ((CrefReferenceSyntax)node).DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                        break;
                }
                break;
            default:
                return false;
        }

        return CanAddImportForTypeOrNamespaceCore(node, out nameNode);
    }

    private static bool CanAddImportForTypeOrNamespaceCore(SyntaxNode node, out SimpleNameSyntax nameNode)
    {
        nameNode = null;

        var qn = node as QualifiedNameSyntax;
        if (qn != null)
        {
            node = GetLeftMostSimpleName(qn);
        }

        nameNode = node as SimpleNameSyntax;
        return nameNode.LooksLikeStandaloneTypeName();
    }

    private static SimpleNameSyntax GetLeftMostSimpleName(QualifiedNameSyntax qn)
    {
        while (qn != null)
        {
            var left = qn.Left;
            var simpleName = left as SimpleNameSyntax;
            if (simpleName != null)
            {
                return simpleName;
            }

            qn = left as QualifiedNameSyntax;
        }

        return null;
    }

    protected override string GetDescription(IReadOnlyList<string> nameParts)
    {
        return $"Imports {string.Join(".", nameParts)}";
    }

    protected override (string description, bool hasExistingImport) GetDescription(
        Document document,
        AddImportPlacementOptions options,
        INamespaceOrTypeSymbol symbol,
        SemanticModel semanticModel,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var importsStatement = GetImportsStatement(symbol);
        var addImportService = document.GetLanguageService<IAddImportsService>();
        var generator = SyntaxGenerator.GetGenerator(document);
        return ($"Imports {symbol.ToDisplayString()}",
                addImportService.HasExistingImport(semanticModel, root, root, importsStatement, generator, cancellationToken));
    }

    private static ImportsStatementSyntax GetImportsStatement(INamespaceOrTypeSymbol symbol)
    {
        var nameSyntax = (NameSyntax)symbol.GenerateTypeSyntax(addGlobal: false);
        return GetImportsStatement(nameSyntax);
    }

    private static ImportsStatementSyntax GetImportsStatement(NameSyntax nameSyntax)
    {
        nameSyntax = nameSyntax.WithAdditionalAnnotations(Simplifier.Annotation);

        var memberImportsClause = SyntaxFactory.SimpleImportsClause(nameSyntax);
        var newImport = SyntaxFactory.ImportsStatement(
            importsClauses: SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(memberImportsClause));

        return newImport;
    }

    protected override ISet<INamespaceSymbol> GetImportNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        return semanticModel.GetImportNamespacesInScope(node);
    }

    protected override ITypeSymbol GetDeconstructInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        return null;
    }

    protected override ITypeSymbol GetQueryClauseInfo(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var query = node as QueryExpressionSyntax;

        if (query == null)
        {
            query = node.GetAncestor<QueryExpressionSyntax>();
        }

        foreach (var clause in query.Clauses)
        {
            if (clause is AggregateClauseSyntax aggregateClause)
            {
                var aggregateInfo = semanticModel.GetAggregateClauseSymbolInfo(aggregateClause, cancellationToken);
                if (IsValid(aggregateInfo.Select1) || IsValid(aggregateInfo.Select2))
                {
                    return null;
                }

                foreach (var variable in aggregateClause.AggregationVariables)
                {
                    var info = semanticModel.GetSymbolInfo(variable.Aggregation, cancellationToken);
                    if (IsValid(info))
                    {
                        return null;
                    }
                }
            }
            else
            {
                var symbolInfo = semanticModel.GetSymbolInfo(clause, cancellationToken);
                if (IsValid(symbolInfo))
                {
                    return null;
                }
            }
        }

        ITypeSymbol type;
        var fromOrAggregateClause = query.Clauses.First();
        if (fromOrAggregateClause is FromClauseSyntax fromClause)
        {
            type = semanticModel.GetTypeInfo(fromClause.Variables.First().Expression, cancellationToken).Type;
        }
        else
        {
            var aggregateClause = (AggregateClauseSyntax)fromOrAggregateClause;
            type = semanticModel.GetTypeInfo(aggregateClause.Variables.First().Expression, cancellationToken).Type;
        }

        return type;
    }

    private static bool IsValid(SymbolInfo info)
    {
        var symbol = info.Symbol.GetOriginalUnreducedDefinition();
        return symbol != null && symbol.Locations.Length > 0;
    }

    protected override async Task<Document> AddImportAsync(
        SyntaxNode contextNode,
        INamespaceOrTypeSymbol symbol,
        Document document,
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        var importsStatement = GetImportsStatement(symbol);

        return await AddImportAsync(contextNode, document, importsStatement, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AddImportAsync(
        SyntaxNode contextNode,
        Document document,
        ImportsStatementSyntax importsStatement,
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var importService = document.GetLanguageService<IAddImportsService>();
        var generator = SyntaxGenerator.GetGenerator(document);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = importService.AddImport(semanticModel, root, contextNode, importsStatement, generator, options, cancellationToken);
        newRoot = newRoot.WithAdditionalAnnotations(CaseCorrector.Annotation, Formatter.Annotation);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }

    protected override Task<Document> AddImportAsync(
        SyntaxNode contextNode,
        IReadOnlyList<string> nameSpaceParts,
        Document document,
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        var nameSyntax = CreateNameSyntax(nameSpaceParts, nameSpaceParts.Count - 1);
        var importsStatement = GetImportsStatement(nameSyntax);

        return AddImportAsync(contextNode, document, importsStatement, options, cancellationToken);
    }

    private static NameSyntax CreateNameSyntax(IReadOnlyList<string> nameSpaceParts, int index)
    {
        var namePiece = SyntaxFactory.IdentifierName(nameSpaceParts[index]);
        return index == 0
            ? (NameSyntax)namePiece
            : SyntaxFactory.QualifiedName(CreateNameSyntax(nameSpaceParts, index - 1), namePiece);
    }

    protected override bool IsAddMethodContext(
        SyntaxNode node,
        SemanticModel semanticModel,
        out SyntaxNode objectCreateExpression)
    {
        objectCreateExpression = null;

        if (node.IsKind(SyntaxKind.ObjectCollectionInitializer))
        {
            objectCreateExpression = node.GetAncestor<ObjectCreationExpressionSyntax>();
            return objectCreateExpression != null;
        }

        return false;
    }
}
