// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(OverrideCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(CompletionListTagCompletionProvider))]
[Shared]
internal sealed class OverrideCompletionProvider : AbstractOverrideCompletionProvider
{
    private bool _isFunction;
    private bool _isSub;
    private bool _isProperty;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OverrideCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    protected override SyntaxNode? GetSyntax(SyntaxToken commonSyntaxToken)
    {
        var token = (SyntaxToken)commonSyntaxToken;

        var propertyBlock = token.GetAncestor<PropertyBlockSyntax>();
        if (propertyBlock != null)
        {
            return propertyBlock;
        }

        var methodBlock = token.GetAncestor<MethodBlockBaseSyntax>();
        if (methodBlock != null)
        {
            return methodBlock;
        }

        return token.GetAncestor<MethodStatementSyntax>();
    }

    protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree syntaxTree, CancellationToken cancellationToken)
    {
        var tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem);
        return syntaxTree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
    }

    public override SyntaxToken FindStartingToken(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        return token.GetPreviousTokenIfTouchingWord(position);
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.SpaceTriggerChar;

    public override bool TryDetermineModifiers(SyntaxToken startToken,
                                              SourceText text, int startLine,
                                              out Accessibility seenAccessibility,
                                              out DeclarationModifiers modifiers)
    {
        var token = (SyntaxToken)startToken;
        modifiers = new DeclarationModifiers();
        seenAccessibility = Accessibility.NotApplicable;
        var overridesToken = new SyntaxToken();
        var isMustOverride = false;
        var isNotOverridable = false;
        _isSub = false;
        _isFunction = false;
        _isProperty = false;

        while (IsOnStartLine(token.SpanStart, text, startLine))
        {
            switch (token.Kind())
            {
                case SyntaxKind.OverridesKeyword:
                    overridesToken = token;
                    break;
                case SyntaxKind.MustOverrideKeyword:
                    isMustOverride = true;
                    break;
                case SyntaxKind.NotOverridableKeyword:
                    isNotOverridable = true;
                    break;
                case SyntaxKind.FunctionKeyword:
                    _isFunction = true;
                    break;
                case SyntaxKind.PropertyKeyword:
                    _isProperty = true;
                    break;
                case SyntaxKind.SubKeyword:
                    _isSub = true;
                    break;

                // Filter on accessibility by keeping the first one that we see
                case SyntaxKind.PublicKeyword:
                    if (seenAccessibility == Accessibility.NotApplicable)
                    {
                        seenAccessibility = Accessibility.Public;
                    }
                    break;

                case SyntaxKind.FriendKeyword:
                    if (seenAccessibility == Accessibility.NotApplicable)
                    {
                        seenAccessibility = Accessibility.Internal;
                    }

                    // If we see Friend AND Protected, assume Friend Protected
                    if (seenAccessibility == Accessibility.Protected)
                    {
                        seenAccessibility = Accessibility.ProtectedOrInternal;
                    }
                    break;

                case SyntaxKind.ProtectedKeyword:
                    if (seenAccessibility == Accessibility.NotApplicable)
                    {
                        seenAccessibility = Accessibility.Protected;
                    }

                    // If we see Protected and Friend, assume Protected Friend
                    if (seenAccessibility == Accessibility.Internal)
                    {
                        seenAccessibility = Accessibility.ProtectedOrInternal;
                    }
                    break;

                default:
                    // If we see anything else, give up
                    return false;
            }

            var previousToken = token.GetPreviousToken();

            // Consume only modifiers on the same line
            if (previousToken.Kind() == SyntaxKind.None || !IsOnStartLine(previousToken.SpanStart, text, startLine))
            {
                break;
            }

            token = previousToken;
        }

        modifiers = new DeclarationModifiers(isAbstract: isMustOverride, isOverride: true, isSealed: isNotOverridable);
        return overridesToken.Kind() == SyntaxKind.OverridesKeyword && IsOnStartLine(overridesToken.Parent!.SpanStart, text, startLine);
    }

    public override bool TryDetermineReturnType(SyntaxToken startToken,
                                               SemanticModel semanticModel,
                                               CancellationToken cancellationToken,
                                               out ITypeSymbol? returnType, out SyntaxToken nextToken)
    {
        nextToken = startToken;
        returnType = null;

        return true;
    }

    public override ImmutableArray<ISymbol> FilterOverrides(ImmutableArray<ISymbol> members,
                                                          ITypeSymbol? returnType)
    {
        // Start by removing Finalize(), which we never want to show.
        var finalizeMethod = members.OfType<IMethodSymbol>().Where(x => x.Name == "Finalize" && OverridesObjectMethod(x)).SingleOrDefault();
        if (finalizeMethod != null)
        {
            members = members.Remove(finalizeMethod);
        }

        if (_isFunction)
        {
            // Function: look for non-void return types
            var filteredMembers = members.OfType<IMethodSymbol>().Where(m => !m.ReturnsVoid);
            if (filteredMembers.Any())
            {
                return ImmutableArray<ISymbol>.CastUp(filteredMembers.ToImmutableArray());
            }
        }
        else if (_isProperty)
        {
            // Property: return properties
            var filteredMembers = members.Where(m => m.Kind == SymbolKind.Property);
            if (filteredMembers.Any())
            {
                return filteredMembers.ToImmutableArray();
            }
        }
        else if (_isSub)
        {
            // Sub: look for void return types
            var filteredMembers = members.OfType<IMethodSymbol>().Where(m => m.ReturnsVoid);
            if (filteredMembers.Any())
            {
                return ImmutableArray<ISymbol>.CastUp(filteredMembers.ToImmutableArray());
            }
        }

        return members.WhereAsArray(m => !m.IsKind(SymbolKind.Event));
    }

    private static bool OverridesObjectMethod(IMethodSymbol method)
    {
        var overriddenMember = method;
        while (overriddenMember.OverriddenMethod != null)
        {
            overriddenMember = overriddenMember.OverriddenMethod;
        }

        if (overriddenMember.ContainingType.SpecialType == SpecialType.System_Object)
        {
            return true;
        }

        return false;
    }

    protected override TextSpan GetTargetSelectionSpan(SyntaxNode caretTarget)
    {
        var node = (SyntaxNode)caretTarget;

        // MustOverride Sub | MustOverride Function: move to end of line
        if (node is MethodStatementSyntax methodStatement)
        {
            return new TextSpan(methodStatement.GetLocation().SourceSpan.End, 0);
        }

        if (node is MethodBlockBaseSyntax methodBlock)
        {
            var lastStatement = methodBlock.Statements.LastOrDefault();
            if (lastStatement != null)
            {
                return new TextSpan(lastStatement.GetLocation().SourceSpan.End, 0);
            }
        }

        if (node is PropertyBlockSyntax propertyBlock)
        {
            var firstAccessor = propertyBlock.Accessors.FirstOrDefault();
            if (firstAccessor != null)
            {
                var lastAccessorStatement = firstAccessor.Statements.LastOrDefault();
                if (lastAccessorStatement != null)
                {
                    return new TextSpan(lastAccessorStatement.GetLocation().SourceSpan.End, 0);
                }
            }
        }

        return new TextSpan(0, 0);
    }
}
