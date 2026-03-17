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
/// Recommends the "Inherits" keyword.
/// </summary>
internal class InheritsKeywordRecommender : AbstractKeywordRecommender
{
    private static readonly ImmutableArray<RecommendedKeyword> s_keywords =
        ImmutableArray.Create(new RecommendedKeyword("Inherits", VBFeaturesResources.Causes_the_current_class_or_interface_to_inherit_the_attributes_variables_properties_procedures_and_events_from_another_class_or_set_of_interfaces));

    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        // Inherits must be the first thing in the class, by rule.
        if (context.IsAfterStatementOfKind(SyntaxKind.ClassStatement, SyntaxKind.InterfaceStatement))
        {
            return s_keywords;
        }

        // Inherits may also after other Inherits statements in an interface
        var typeBlock = context.TargetToken.GetAncestor<TypeBlockSyntax>();
        if (context.IsAfterStatementOfKind(SyntaxKind.InheritsStatement) &&
            typeBlock is InterfaceBlockSyntax)
        {
            return s_keywords;
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }
}
