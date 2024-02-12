// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private SyntaxNode[] _largeRootsUnpopulated;
        private SyntaxNode _largeRootPopulated;
        private readonly int _loopCount = 100;

        [Params(false, true)]
        public bool UseNew { get; set; }

        [Params(false, true)]
        public bool UsePopulated { get; set; }

        public DescendantNodesBenchmark()
        {
            _largeSourceText = null!;
            _largeRootsUnpopulated = null!;
            _largeRootPopulated = null!;
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var largeFileText = System.IO.File.ReadAllText(@"d:\sources\Roslyn\src\Compilers\CSharp\Portable\Parser\LanguageParser.cs");
            _largeSourceText = SourceText.From(largeFileText);

            _largeRootPopulated = CSharpSyntaxTree.ParseText(_largeSourceText).GetRoot();
            _largeRootsUnpopulated = new SyntaxNode[_loopCount];
        }

        [IterationSetup]
        public void IterationSetup()
        {
            if (!UsePopulated)
            {
                for (var i = 0; i < _loopCount; i++)
                    _largeRootsUnpopulated[i] = CSharpSyntaxTree.ParseText(_largeSourceText).GetRoot();
            }
        }

        [Benchmark]
        public void FullDocumentWalk()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _largeRootPopulated : _largeRootsUnpopulated[i];
                if (UseNew)
                    root.DescendantNodes2().ToList();
                else
                    root.DescendantNodes().ToList();
            }
        }

        [Benchmark]
        public void KindSearch()
        {
            for (var i = 0; i < _loopCount; i++)
            {
                var root = UsePopulated ? _largeRootPopulated : _largeRootsUnpopulated[i];
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

        [IterationCleanup]
        public void Cleanup()
        {
        }
    }
}
