// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Event" keyword in type declaration contexts
/// </summary>
internal class EventKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Event", VBFeaturesResources.Declares_a_user_defined_event));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeMemberDeclarationKeywordContext || context.IsInterfaceMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Event))
            {
                return s_keywords;
            }
        }

        // We also allow "Event" after Custom (which is parsed as an identifier) in a class/structure declaration context
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (targetToken.Kind() == SyntaxKind.IdentifierToken && SyntaxFacts.GetContextualKeywordKind(targetToken.GetIdentifierText()) == SyntaxKind.CustomKeyword)
        {
            if (targetToken.GetAncestor<MethodBlockBaseSyntax>() == null &&
                targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.StructureBlock, SyntaxKind.ClassBlock))
            {
                var variableDeclarator = targetToken.GetAncestor<VariableDeclaratorSyntax>();
                if (variableDeclarator != null)
                {
                    if (variableDeclarator.Names.Count == 1 && variableDeclarator.Names.First().Identifier == targetToken)
                    {
                        return s_keywords;
                    }
                }
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
