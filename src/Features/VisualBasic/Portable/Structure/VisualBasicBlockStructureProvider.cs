// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Structure;

internal sealed class VisualBasicBlockStructureProvider : AbstractBlockStructureProvider
{
    public static ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultNodeStructureProviderMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, ImmutableArray<AbstractSyntaxStructureProvider>>();

        builder.Add<AccessorStatementSyntax, AccessorDeclarationStructureProvider>();
        builder.Add<ClassStatementSyntax, TypeDeclarationStructureProvider>();
        builder.Add<CollectionInitializerSyntax, CollectionInitializerStructureProvider>();
        builder.Add<CompilationUnitSyntax, CompilationUnitStructureProvider>();
        builder.Add<SubNewStatementSyntax, ConstructorDeclarationStructureProvider>();
        builder.Add<DelegateStatementSyntax, DelegateDeclarationStructureProvider>();
        builder.Add<DocumentationCommentTriviaSyntax, DocumentationCommentStructureProvider>();
        builder.Add<DoLoopBlockSyntax, DoLoopBlockStructureProvider>();
        builder.Add<EnumStatementSyntax, EnumDeclarationStructureProvider>();
        builder.Add<EnumMemberDeclarationSyntax, EnumMemberDeclarationStructureProvider>();
        builder.Add<EventStatementSyntax, EventDeclarationStructureProvider>();
        builder.Add<DeclareStatementSyntax, ExternalMethodDeclarationStructureProvider>();
        builder.Add<FieldDeclarationSyntax, FieldDeclarationStructureProvider>();
        builder.Add<ForBlockSyntax, ForBlockStructureProvider>();
        builder.Add<ForEachBlockSyntax, ForEachBlockStructureProvider>();
        builder.Add<InterfaceStatementSyntax, TypeDeclarationStructureProvider>();
        builder.Add<MethodStatementSyntax, MethodDeclarationStructureProvider>();
        builder.Add<ModuleStatementSyntax, TypeDeclarationStructureProvider>();
        builder.Add<MultiLineIfBlockSyntax, MultiLineIfBlockStructureProvider>();
        builder.Add<MultiLineLambdaExpressionSyntax, MultilineLambdaStructureProvider>();
        builder.Add<NamespaceStatementSyntax, NamespaceDeclarationStructureProvider>();
        builder.Add<ObjectCollectionInitializerSyntax, ObjectCreationInitializerStructureProvider>();
        builder.Add<ObjectMemberInitializerSyntax, ObjectCreationInitializerStructureProvider>();
        builder.Add<OperatorStatementSyntax, OperatorDeclarationStructureProvider>();
        builder.Add<PropertyStatementSyntax, PropertyDeclarationStructureProvider>();
        builder.Add<RegionDirectiveTriviaSyntax, RegionDirectiveStructureProvider>();
        builder.Add<SelectBlockSyntax, SelectBlockStructureProvider>();
        builder.Add<StructureStatementSyntax, TypeDeclarationStructureProvider>();
        builder.Add<SyncLockBlockSyntax, SyncLockBlockStructureProvider>();
        builder.Add<TryBlockSyntax, TryBlockStructureProvider>();
        builder.Add<UsingBlockSyntax, UsingBlockStructureProvider>();
        builder.Add<WhileBlockSyntax, WhileBlockStructureProvider>();
        builder.Add<WithBlockSyntax, WithBlockStructureProvider>();
        builder.Add<XmlCDataSectionSyntax, XmlExpressionStructureProvider>();
        builder.Add<XmlCommentSyntax, XmlExpressionStructureProvider>();
        builder.Add<XmlDocumentSyntax, XmlExpressionStructureProvider>();
        builder.Add<XmlElementSyntax, XmlExpressionStructureProvider>();
        builder.Add<XmlProcessingInstructionSyntax, XmlExpressionStructureProvider>();
        builder.Add<LiteralExpressionSyntax, StringLiteralExpressionStructureProvider>();
        builder.Add<InterpolatedStringExpressionSyntax, InterpolatedStringExpressionStructureProvider>();

        return builder.ToImmutable();
    }

    public static ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultTriviaStructureProviderMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<int, ImmutableArray<AbstractSyntaxStructureProvider>>();

        builder.Add((int)SyntaxKind.DisabledTextTrivia, [new DisabledTextTriviaStructureProvider()]);

        return builder.ToImmutable();
    }

    internal VisualBasicBlockStructureProvider()
        : base(CreateDefaultNodeStructureProviderMap(), CreateDefaultTriviaStructureProviderMap())
    {
    }
}
