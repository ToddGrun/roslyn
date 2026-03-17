// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations;

/// <summary>
/// Recommends "End [block]" or, if after a End keyword, just the Block.
/// </summary>
internal class EndBlockKeywordRecommender : AbstractKeywordRecommender
{
    protected override ImmutableArray<RecommendedKeyword> RecommendKeywords(VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsPreProcessorDirectiveContext)
        {
            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        var targetToken = context.TargetToken;

        if (targetToken.IsKind(SyntaxKind.EndKeyword) &&
            (targetToken.IsChildToken<EndBlockStatementSyntax>(endBlock => endBlock.EndKeyword) ||
             targetToken.IsChildToken<StopOrEndStatementSyntax>(endStatement => endStatement.StopOrEndKeyword)))
        {
            // We already have "End", so just recommend the closeable things

            // NOTE: use the token to the left of "End" to locate parenting blocks, otherwise, this won't work
            // for blocks that can't contain an End statement. For example, Enums and Interfaces -- in these cases
            // the End statement is located outside of the block.
            targetToken = targetToken.GetPreviousToken();

            var keywords = from keyword in GetUnclosedBlockKeywords(targetToken.Parent)
                           select SyntaxFacts.GetText(keyword);

            var keywordList = keywords.ToList();
            EnsureAllIfAny(keywordList, "Function", "Sub");
            return keywordList.SelectAsArray(k => new RecommendedKeyword(k, GetToolTipForKeyword(k)));
        }
        else if (context.FollowsEndOfStatement)
        {
            // If you're in a case like this
            //
            //     End If
            //     |
            //
            // our target token is the "If", even though it's closed. So we want to skip to the parent of the If
            // block to start to figure out what blocks we still have
            var node = targetToken.Parent;

            if (node is EndBlockStatementSyntax)
            {
                node = node.Parent.Parent;
            }

            if (node != null)
            {
                // We don't have "End", so recommend everything with the End keyword
                return GetUnclosedBlockKeywords(node).SelectAsArray(
                    k => new RecommendedKeyword("End " + SyntaxFacts.GetText(k), GetToolTipForKeyword(SyntaxFacts.GetText(k))));
            }
        }

        return ImmutableArray<RecommendedKeyword>.Empty;
    }

    private static string GetToolTipForKeyword(string keyword)
    {
        switch (keyword)
        {
            case "Region":
            case "Class":
            case "Structure":
            case "Namespace":
            case "Module":
                return string.Format(VBFeaturesResources.Terminates_a_0_block, keyword);
            case "Interface":
            case "Enum":
                return string.Format(VBFeaturesResources.Terminates_an_0_block, keyword);
            case "Select":
                return string.Format(VBFeaturesResources.Terminates_the_definition_of_a_0_statement, keyword + " Case");
            case "SyncLock":
            case "Try":
            case "Using":
            case "While":
            case "With":
            case "Sub":
            case "Function":
            case "Set":
            case "Get":
            case "RemoveHandler":
            case "RaiseEvent":
                return string.Format(VBFeaturesResources.Terminates_the_definition_of_a_0_statement, keyword);
            case "If":
            case "Operator":
            case "AddHandler":
                return string.Format(VBFeaturesResources.Terminates_the_definition_of_an_0_statement, keyword);
            default:
                return string.Empty;
        }
    }

    private static void EnsureAllIfAny(ICollection<string> collection, params string[] completions)
    {
        foreach (var item in completions)
        {
            if (collection.Contains(item))
            {
                foreach (var item2 in completions)
                {
                    if (!collection.Contains(item2))
                    {
                        collection.Add(item2);
                    }
                }

                break;
            }
        }
    }

    private static IEnumerable<SyntaxKind> GetUnclosedBlockKeywords(SyntaxNode node)
    {
        var visitor = new MissingKeywordExtractor();

        return from ancestor in node.GetAncestorsOrThis<SyntaxNode>()
               let missingKeyword = visitor.Visit(ancestor)
               where missingKeyword.HasValue
               select missingKeyword.Value;
    }

    private class MissingKeywordExtractor : VisualBasicSyntaxVisitor<SyntaxKind?>
    {
        public override SyntaxKind? VisitNamespaceBlock(NamespaceBlockSyntax node)
        {
            if (node.EndNamespaceStatement.IsMissing)
            {
                return SyntaxKind.NamespaceKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitModuleBlock(ModuleBlockSyntax node)
        {
            if (node.EndBlockStatement.IsMissing)
            {
                return SyntaxKind.ModuleKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitClassBlock(ClassBlockSyntax node)
        {
            if (node.EndBlockStatement.IsMissing)
            {
                return SyntaxKind.ClassKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitStructureBlock(StructureBlockSyntax node)
        {
            if (node.EndBlockStatement.IsMissing)
            {
                return SyntaxKind.StructureKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitInterfaceBlock(InterfaceBlockSyntax node)
        {
            if (node.EndBlockStatement.IsMissing)
            {
                return SyntaxKind.InterfaceKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitEnumBlock(EnumBlockSyntax node)
        {
            if (node.EndEnumStatement.IsMissing)
            {
                return SyntaxKind.EnumKeyword;
            }
            else
            {
                return null;
            }
        }

        private static SyntaxKind? VisitMethodBlockBase(MethodBlockBaseSyntax node)
        {
            if (node.EndBlockStatement.IsMissing)
            {
                return node.BlockStatement.DeclarationKeyword.Kind();
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitMethodBlock(MethodBlockSyntax node)
            => VisitMethodBlockBase(node);

        public override SyntaxKind? VisitConstructorBlock(ConstructorBlockSyntax node)
            => VisitMethodBlockBase(node);

        public override SyntaxKind? VisitOperatorBlock(OperatorBlockSyntax node)
            => VisitMethodBlockBase(node);

        public override SyntaxKind? VisitAccessorBlock(AccessorBlockSyntax node)
            => VisitMethodBlockBase(node);

        public override SyntaxKind? VisitMultiLineIfBlock(MultiLineIfBlockSyntax node)
        {
            if (node.EndIfStatement.IsMissing)
            {
                return SyntaxKind.IfKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitPropertyBlock(PropertyBlockSyntax node)
        {
            if (node.EndPropertyStatement.IsMissing)
            {
                return node.PropertyStatement.DeclarationKeyword.Kind();
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitSyncLockBlock(SyncLockBlockSyntax node)
        {
            if (node.EndSyncLockStatement.IsMissing)
            {
                return SyntaxKind.SyncLockKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitSelectBlock(SelectBlockSyntax node)
        {
            if (node.EndSelectStatement.IsMissing)
            {
                return SyntaxKind.SelectKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitUsingBlock(UsingBlockSyntax node)
        {
            if (node.EndUsingStatement.IsMissing)
            {
                return SyntaxKind.UsingKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitWhileBlock(WhileBlockSyntax node)
        {
            if (node.EndWhileStatement.IsMissing)
            {
                return SyntaxKind.WhileKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitWithBlock(WithBlockSyntax node)
        {
            if (node.EndWithStatement.IsMissing)
            {
                return SyntaxKind.WithKeyword;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxKind? VisitTryBlock(TryBlockSyntax node)
        {
            if (node.EndTryStatement.IsMissing)
            {
                return SyntaxKind.TryKeyword;
            }
            else
            {
                return null;
            }
        }
    }
}
