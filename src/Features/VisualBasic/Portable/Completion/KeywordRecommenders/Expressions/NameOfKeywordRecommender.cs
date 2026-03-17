// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions;

internal class NameOfKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsAnyExpressionContext)
        {
            return ImmutableArray.Create(CreateRecommendedKeywordForIntrinsicOperator(
                SyntaxKind.NameOfKeyword,
                VBFeaturesResources.NameOf_function,
                Glyph.MethodPublic,
                new NameOfExpressionDocumentation(),
                context.SemanticModel,
                context.Position));
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
