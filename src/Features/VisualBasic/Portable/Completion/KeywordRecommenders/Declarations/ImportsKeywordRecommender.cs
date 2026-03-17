// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends the "Imports" keyword
/// </summary>
internal class ImportsKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Imports", VBFeaturesResources.Imports_all_or_specified_elements_of_a_namespace_into_a_file));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreProcessorDirectiveContext)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        if (context.SyntaxTree.HasCompilationUnitRoot)
        {
            // Make sure there isn't a Option statement after us
            // TODO: does this break our rule of not looking forward?
            var compilationUnit = (CompilationUnitSyntax)context.SyntaxTree.GetRoot(cancellationToken);
            if (compilationUnit.Options.Count > 0)
            {
                if (context.Position <= compilationUnit.Options.First().SpanStart)
                {
                    return ImmutableArray<RecommendedKeyword>.Empty;
                }
            }
        }

        // If we have no left token, then we're at the start of the file
        if (targetToken.Kind() == SyntaxKind.None)
        {
            return s_keywords;
        }

        // Show if after an earlier option statement
        if (context.IsAfterStatementOfKind(SyntaxKind.OptionStatement, SyntaxKind.ImportsStatement))
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
