// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "To" keyword in array bounds.
/// </summary>
internal class ToKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("To", VBFeaturesResources.Separates_the_beginning_and_ending_values_of_a_loop_counter_or_array_bounds_or_that_of_a_value_match_range));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        var simpleArgument = targetToken.GetAncestor<SimpleArgumentSyntax>();
        if (simpleArgument != null)
        {
            var modifiedIdentifier = targetToken.GetAncestor<ModifiedIdentifierSyntax>();
            if (modifiedIdentifier != null)
            {
                if (modifiedIdentifier.ArrayBounds != null &&
                    modifiedIdentifier.ArrayBounds.Arguments.Contains(simpleArgument))
                {
                    return s_keywords;
                }
            }

            // For ReDim, this will be a ReDim clause.
            var clause = targetToken.GetAncestor<RedimClauseSyntax>();
            if (clause != null)
            {
                var redimStatement = targetToken.GetAncestor<ReDimStatementSyntax>();
                if (redimStatement != null)
                {
                    if (redimStatement.Clauses.Contains(clause))
                    {
                        return s_keywords;
                    }
                }
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
