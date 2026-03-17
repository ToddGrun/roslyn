// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Types;

/// <summary>
/// Recommends built-in types in various contexts.
/// </summary>
internal class BuiltInTypesKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTaskLikeTypeContext)
        {
            return [];
        }

        var targetToken = context.TargetToken;

        // Are we right after an As in an Enum declaration?
        if (context.IsEnumBaseListContext)
        {
            var keywordList = GetIntrinsicTypeKeywords(context);

            return keywordList.WhereAsArray(
                k => k.Keyword.EndsWith("Byte", StringComparison.Ordinal) ||
                            k.Keyword.EndsWith("Short", StringComparison.Ordinal) ||
                            k.Keyword.EndsWith("Integer", StringComparison.Ordinal) ||
                            k.Keyword.EndsWith("Long", StringComparison.Ordinal));
        }

        // Are we inside a type constraint? Because these are never allowed there
        if (targetToken.GetAncestor<TypeParameterSingleConstraintClauseSyntax>() is not null ||
           targetToken.GetAncestor<TypeParameterMultipleConstraintClauseSyntax>() is not null)
        {
            return [];
        }

        // Are we inside an attribute block? They're at least not allowed as the attribute itself
        if (targetToken.Parent.IsKind(SyntaxKind.AttributeList))
        {
            return [];
        }

        // Are we in an Imports statement? Type keywords aren't allowed there, just fully qualified type names
        if (targetToken.GetAncestor<ImportsStatementSyntax>() is not null)
        {
            return [];
        }

        // Are we after Inherits or Implements? Type keywords aren't allowed here.
        if (targetToken.IsChildToken<InheritsStatementSyntax>(static n => n.InheritsKeyword) ||
           targetToken.IsChildToken<ImplementsStatementSyntax>(static n => n.ImplementsKeyword))
        {
            return [];
        }

        if (context.IsTypeContext)
        {
            return GetIntrinsicTypeKeywords(context);
        }

        return [];
    }

    private static readonly string[] s_intrinsicKeywordNames =
    [
        "Boolean",
        "Byte",
        "Char",
        "Date",
        "Decimal",
        "Double",
        "Integer",
        "Long",
        "Object",
        "SByte",
        "Short",
        "Single",
        "String",
        "UInteger",
        "ULong",
        "UShort"
    ];

    private static readonly sbyte[] s_intrinsicSpecialTypes =
    [
        (sbyte)SpecialType.System_Boolean,
        (sbyte)SpecialType.System_Byte,
        (sbyte)SpecialType.System_Char,
        (sbyte)SpecialType.System_DateTime,
        (sbyte)SpecialType.System_Decimal,
        (sbyte)SpecialType.System_Double,
        (sbyte)SpecialType.System_Int32,
        (sbyte)SpecialType.System_Int64,
        (sbyte)SpecialType.System_Object,
        (sbyte)SpecialType.System_SByte,
        (sbyte)SpecialType.System_Int16,
        (sbyte)SpecialType.System_Single,
        (sbyte)SpecialType.System_String,
        (sbyte)SpecialType.System_UInt32,
        (sbyte)SpecialType.System_UInt64,
        (sbyte)SpecialType.System_UInt16
    ];

    private static ImmutableArray<RecommendedKeyword> GetIntrinsicTypeKeywords(VisualBasicSyntaxContext context)
    {
        Debug.Assert(s_intrinsicKeywordNames.Length == s_intrinsicSpecialTypes.Length);

        var inferredSpecialTypes = context.InferredTypes.Select(t => t.SpecialType).ToSet();

        var recommendedKeywords = new RecommendedKeyword[s_intrinsicKeywordNames.Length];
        for (var i = 0; i < s_intrinsicKeywordNames.Length; i++)
        {
            var keyword = s_intrinsicKeywordNames[i];
            var specialType = (SpecialType)s_intrinsicSpecialTypes[i];

            var priority = inferredSpecialTypes.Contains(specialType) ? SymbolMatchPriority.Keyword : MatchPriority.Default;

            recommendedKeywords[i] = new RecommendedKeyword(s_intrinsicKeywordNames[i], Glyph.Keyword,
                                                            cancellationToken =>
                                                            {
                                                                var tooltip = GetDocumentationCommentText(context, specialType, cancellationToken);
                                                                return RecommendedKeyword.CreateDisplayParts(keyword, tooltip);
                                                            }, isIntrinsic: true, matchPriority: priority);
        }

        return recommendedKeywords.ToImmutableArray();
    }

    private static string GetDocumentationCommentText(VisualBasicSyntaxContext context, SpecialType type, CancellationToken cancellationToken)
    {
        var symbol = context.SemanticModel.Compilation.GetSpecialType(type);
        return symbol.GetDocumentationComment(context.SemanticModel.Compilation, System.Globalization.CultureInfo.CurrentUICulture, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).SummaryText;
    }
}
