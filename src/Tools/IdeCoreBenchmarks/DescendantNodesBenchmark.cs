// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class DescendantNodesBenchmark
    {
        private SourceText _largeSourceText;
        private SourceText _smallSourceText;
        private SyntaxNode _largeRootPopulated;
        private SyntaxNode _smallRootPopulated;

        private SyntaxNode _rootPopulated;
        private SyntaxNode[] _rootsUnpopulated;

        private readonly int _loopCount = 100;

        [Params(false, true)]
        public bool UsePopulated { get; set; }

        [Params(false, true)]
        public bool IsLarge { get; set; }

        [Params(false, true)]
        public bool UseNew { get; set; }

        public DescendantNodesBenchmark()
        {
            _largeSourceText = null!;
            _smallSourceText = null!;
            _rootsUnpopulated = null!;
            _rootPopulated = null!;
            _largeRootPopulated = null!;
            _smallRootPopulated = null!;
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var largeFileText = System.IO.File.ReadAllText(@"c:\sources\Roslyn\src\Compilers\CSharp\Portable\Parser\LanguageParser.cs");
            _largeSourceText = SourceText.From(largeFileText);
            _largeRootPopulated = CSharpSyntaxTree.ParseText(_largeSourceText).GetRoot();

            var smallFileText = System.IO.File.ReadAllText(@"c:\sources\Roslyn\src\Compilers\CSharp\Portable\Parser\QuickScanner.cs");
            _smallSourceText = SourceText.From(smallFileText);
            _smallRootPopulated = CSharpSyntaxTree.ParseText(_smallSourceText).GetRoot();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            if (UsePopulated)
            {
                _rootPopulated = IsLarge ? _largeRootPopulated : _smallRootPopulated;
            }
            else
            {
                _rootsUnpopulated = new SyntaxNode[_loopCount];
                var sourceText = IsLarge ? _largeSourceText : _smallSourceText;
                for (var i = 0; i < _loopCount; i++)
                    _rootsUnpopulated[i] = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            }
        }

        [Benchmark]
        public void AllNodes()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _rootPopulated : _rootsUnpopulated[i];
                if (UseNew)
                    root.DescendantNodes2().ToList();
                else
                    root.DescendantNodes().ToList();
            }
        }

        [Benchmark]
        public void Directives()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _rootPopulated : _rootsUnpopulated[i];
                if (UseNew)
                    root.DescendantNodes2(getNodeBehavior).ToList();
                else
                    root.DescendantNodes(descendIntoChildren).ToList();
            }

            SyntaxNode.NodeTraversalBehavior getNodeBehavior(SyntaxNode.NodeTraversalContext context)
            {
                var traverseInsideResult = context.ContainsDirectives ? SyntaxNode.NodeTraversalBehavior.TraverseInside : 0;

                return traverseInsideResult | SyntaxNode.NodeTraversalBehavior.IncludeInResult;
            }

            bool descendIntoChildren(SyntaxNode node)
            {
                return node.ContainsDirectives;
            }
        }

        [Benchmark]
        public void CommonKind()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _rootPopulated : _rootsUnpopulated[i];
                if (UseNew)
                    root.DescendantNodes2(getNodeBehavior).ToList();
                else
                    root.DescendantNodes().Where(n => n is ConditionalExpressionSyntax).ToList();
            }

            SyntaxNode.NodeTraversalBehavior getNodeBehavior(SyntaxNode.NodeTraversalContext context)
            {
                var includeInResult = context.Kind == (int)SyntaxKind.ConditionalExpression ? SyntaxNode.NodeTraversalBehavior.IncludeInResult : 0;

                return includeInResult | SyntaxNode.NodeTraversalBehavior.TraverseInside;
            }
        }

        [Benchmark]
        public void UncommonKind()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _rootPopulated : _rootsUnpopulated[i];
                if (UseNew)
                    root.DescendantNodes2(getNodeBehavior).ToList();
                else
                    root.DescendantNodes().Where(n => n is FunctionPointerTypeSyntax).ToList();
            }

            SyntaxNode.NodeTraversalBehavior getNodeBehavior(SyntaxNode.NodeTraversalContext context)
            {
                var includeInResult = context.Kind == (int)SyntaxKind.ConditionalExpression ? SyntaxNode.NodeTraversalBehavior.IncludeInResult : 0;

                return includeInResult | SyntaxNode.NodeTraversalBehavior.TraverseInside;
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
        }
    }
}
