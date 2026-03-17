// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(ObjectInitializerCompletionProvider), LanguageNames.VisualBasic), Shared]
[ExtensionOrder(After = nameof(PreprocessorCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ObjectInitializerCompletionProvider() : AbstractObjectInitializerCompletionProvider
{
    internal override string Language => LanguageNames.VisualBasic;

    protected override HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        // The dot wasn't part of the identifier, so move over one more to get the , or {
        if (token.Kind() == SyntaxKind.DotToken)
        {
            token = token.GetPreviousToken();
        }

        if (token.Kind() != SyntaxKind.CommaToken && token.Kind() != SyntaxKind.OpenBraceToken)
        {
            return new HashSet<string>();
        }

        var initializer = token.Parent as ObjectMemberInitializerSyntax;
        if (initializer == null)
        {
            return new HashSet<string>();
        }

        return new HashSet<string>(initializer.Initializers.OfType<NamedFieldInitializerSyntax>().Select(i => i.Name.Identifier.ValueText));
    }

    protected override (ITypeSymbol Type, Location location, bool isObjectInitializer)? GetInitializedType(
        Document document,
        SemanticModel semanticModel,
        int position,
        CancellationToken cancellationToken)
    {
        var tree = semanticModel.SyntaxTree;
        if (tree.IsInNonUserCode(position, cancellationToken))
        {
            return null;
        }

        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        // We should have gotten a ".", since that all we want to come up on
        if (token.Kind() != SyntaxKind.DotToken)
        {
            return null;
        }

        // The dot must be following a comma or open brace
        var commaOrBrace = token.GetPreviousToken();
        if (commaOrBrace.Kind() != SyntaxKind.CommaToken && commaOrBrace.Kind() != SyntaxKind.OpenBraceToken)
        {
            return null;
        }

        // We have the right tokens. Get the containing object initializer. Will we be able to walk
        // up and determine the type?
        var containingInitializer = commaOrBrace.Parent;
        if (containingInitializer == null ||
            containingInitializer.Parent == null ||
            containingInitializer.Kind() != SyntaxKind.ObjectMemberInitializer)
        {
            return null;
        }

        // Get the grandparent object creation expression
        var objectCreationExpression = containingInitializer.Parent as ObjectCreationExpressionSyntax;
        if (objectCreationExpression == null)
        {
            return null;
        }

        var initializerLocation = token.GetLocation();
        var symbolInfo = semanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken);
        var symbol = symbolInfo.Symbol as ITypeSymbol;
        return (symbol, initializerLocation, isObjectInitializer: true);
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return text[characterPosition] == '.';
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create('.');

    protected override Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken)
    {
        // Object initializers are explicitly indicated by "With", so we're always exclusive.
        return SpecializedTasks.True;
    }

    protected override bool IsInitializableFieldOrProperty(ISymbol fieldOrProperty, INamedTypeSymbol containingType)
    {
        // Unlike CSharp, we don't want to suggest readonly members, even if they are Collections
        return base.IsInitializableFieldOrProperty(fieldOrProperty, containingType) &&
            fieldOrProperty.IsWriteableFieldOrProperty() &&
            IsValidProperty(fieldOrProperty);
    }

    protected override string EscapeIdentifier(ISymbol symbol)
    {
        return symbol.Name.EscapeIdentifier();
    }

    private static bool IsValidProperty(ISymbol member)
    {
        var property = member as IPropertySymbol;
        if (property != null)
        {
            return property.Parameters.IsDefaultOrEmpty || property.Parameters.All(p => p.IsOptional || p.IsParams);
        }

        return true;
    }
}
