// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

internal class CastOperatorsKeywordRecommender : AbstractKeywordRecommender
{
    internal static readonly SyntaxKind[] PredefinedKeywordList = [
        SyntaxKind.CBoolKeyword,
        SyntaxKind.CByteKeyword,
        SyntaxKind.CCharKeyword,
        SyntaxKind.CDateKeyword,
        SyntaxKind.CDblKeyword,
        SyntaxKind.CDecKeyword,
        SyntaxKind.CIntKeyword,
        SyntaxKind.CLngKeyword,
        SyntaxKind.CObjKeyword,
        SyntaxKind.CSByteKeyword,
        SyntaxKind.CShortKeyword,
        SyntaxKind.CSngKeyword,
        SyntaxKind.CStrKeyword,
        SyntaxKind.CUIntKeyword,
        SyntaxKind.CULngKeyword,
        SyntaxKind.CUShortKeyword
    ];

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsAnyExpressionContext || context.IsStatementContext)
        {
            var recommendedKeywords = new List<RecommendedKeyword>();

            foreach (var keyword in PredefinedKeywordList)
            {
                recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                    keyword,
                    string.Format(VBFeaturesResources._0_function, SyntaxFacts.GetText(keyword)),
                    Glyph.MethodPublic,
                    new PredefinedCastExpressionDocumentation(keyword, context.SemanticModel.Compilation),
                    context.SemanticModel,
                    context.Position));
            }

            recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                SyntaxKind.CTypeKeyword,
                VBFeaturesResources.CType_function,
                Glyph.MethodPublic,
                new CTypeCastExpressionDocumentation()));

            recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                SyntaxKind.DirectCastKeyword,
                VBFeaturesResources.DirectCast_function,
                Glyph.MethodPublic,
                new DirectCastExpressionDocumentation()));

            recommendedKeywords.Add(CreateRecommendedKeywordForIntrinsicOperator(
                SyntaxKind.TryCastKeyword,
                VBFeaturesResources.TryCast_function,
                Glyph.MethodPublic,
                new TryCastExpressionDocumentation()));

            return recommendedKeywords.ToImmutableArray();
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
