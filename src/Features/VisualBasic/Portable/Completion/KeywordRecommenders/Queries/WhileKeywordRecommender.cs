// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries;

/// <summary>
/// Recommends the While keyword after a Skip/Take query
/// </summary>
internal class WhileKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("While", VBFeaturesResources.Specifies_a_condition_for_Skip_and_Take_operations_Elements_will_be_bypassed_or_included_as_long_as_the_condition_is_true)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;

        // We may get two different types, depending on whether the user has already typed the While or not.
        if (targetToken.IsChildToken<PartitionClauseSyntax>(static partitionQuery => partitionQuery.SkipOrTakeKeyword) ||
           targetToken.IsChildToken<PartitionWhileClauseSyntax>(static partitionWhileQuery => partitionWhileQuery.SkipOrTakeKeyword))
        {
            return s_keywords;
        }

        return [];
    }
}
