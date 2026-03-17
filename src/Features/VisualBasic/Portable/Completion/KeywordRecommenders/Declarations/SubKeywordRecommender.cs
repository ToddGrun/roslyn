// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Sub" keyword in member declaration contexts
/// </summary>
internal class SubKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeMemberDeclarationKeywordContext || context.IsInterfaceMemberDeclarationKeywordContext)
        {
            var modifiers = context.ModifierCollectionFacts;
            if (modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Method) &&
                modifiers.IteratorKeyword.Kind() == SyntaxKind.None)
            {
                return ImmutableArray.Create(new RecommendedKeyword("Sub", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Sub_procedure_that_is_a_procedure_that_does_not_return_a_value_to_the_calling_code));
            }
        }

        // Exit Sub
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        if (targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) &&
            context.IsInStatementBlockOfKind(SyntaxKind.SubBlock, SyntaxKind.MultiLineSubLambdaExpression) &&
            !context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock))
        {
            return ImmutableArray.Create(new RecommendedKeyword("Sub", VBFeaturesResources.Exits_a_Sub_procedure_and_transfers_execution_immediately_to_the_statement_following_the_call_to_the_Sub_procedure));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
