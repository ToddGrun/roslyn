// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements;

/// <summary>
/// Recommends the "Option" keyword
/// </summary>
internal class OptionKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        [new RecommendedKeyword("Option", VBFeaturesResources.Introduces_a_statement_that_specifies_a_compiler_option_that_applies_to_the_entire_source_file)];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreProcessorDirectiveContext)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        // If we have no left token, then we're at the start of the file
        if (targetToken.Kind == SyntaxKind.None)
        {
            return s_keywords;
        }

        // Show if after an earlier option statement
        if (context.IsAfterStatementOfKind(SyntaxKind.OptionStatement))
        {
            return s_keywords;
        }

        return [];
    }
}
