// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling;

/// <summary>
/// Recommends the "RaiseEvent" keyword.
/// </summary>
internal class RaiseEventKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsStatementContext || context.IsMultiLineStatementContext)
        {
            return ImmutableArray.Create(new RecommendedKeyword("RaiseEvent", VBFeaturesResources.Triggers_an_event_declared_at_module_level_within_a_class_form_or_document_RaiseEvent_eventName_bracket_argumentList_bracket));
        }
        else if (context.CanDeclareCustomEventAccessor(SyntaxKind.RaiseEventAccessorBlock))
        {
            return ImmutableArray.Create(new RecommendedKeyword("RaiseEvent", VBFeaturesResources.Specifies_the_statements_to_run_when_the_event_is_raised_by_the_RaiseEvent_statement_RaiseEvent_delegateSignature_End_RaiseEvent));
        }
        else
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }
    }
}
