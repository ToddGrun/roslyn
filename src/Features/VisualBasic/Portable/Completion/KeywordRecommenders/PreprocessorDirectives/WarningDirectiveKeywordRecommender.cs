// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives;

/// <summary>
/// Recommends the "#Disable" preprocessor directive
/// </summary>
internal class WarningDirectiveKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreprocessorStartContext && !context.SyntaxTree.IsEnumMemberNameContext(context))
        {
            return
            [
                new RecommendedKeyword("#Enable Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line),
                new RecommendedKeyword("#Disable Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line)
            ];
        }
        else if (context.IsPreProcessorDirectiveContext)
        {
            if (context.TargetToken.IsKind(SyntaxKind.EnableKeyword))
            {
                return [new RecommendedKeyword("Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line)];
            }
            else if (context.TargetToken.IsKind(SyntaxKind.DisableKeyword))
            {
                return [new RecommendedKeyword("Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line)];
            }
        }

        return [];
    }
}
