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
/// Recommends the "Function" and "Sub" keywords in external method declarations.
/// </summary>
internal class ExternalSubFunctionKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords = ImmutableArray.Create(
        new RecommendedKeyword("Function", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Function),
        new RecommendedKeyword("Sub", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Sub));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.FollowsEndOfStatement)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;
        return targetToken.IsKind(SyntaxKind.DeclareKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.AutoKeyword) && targetToken.GetAncestor<DeclareStatementSyntax>() != null
            ? s_keywords
            : ImmutableArray<RecommendedKeyword>.Empty;
    }
}
