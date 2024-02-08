// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal record NodeOperations : IDisposable
    {
        public static readonly ObjectPool<List<IndentBlockOperation>> IndentBlockOperationPool = new(factory: () => new());
        public static readonly ObjectPool<List<SuppressOperation>> SuppressOperationPool = new(factory: () => new());
        public static readonly ObjectPool<List<AlignTokensOperation>> AlignTokensOperationPool = new(factory: () => new());
        public static readonly ObjectPool<List<AnchorIndentationOperation>> AnchorIndentationOperationPool = new(factory: () => new());

        public static NodeOperations Empty = new();

        public List<IndentBlockOperation> IndentBlockOperation { get; } = = IndentBlockOperationPool.Allocate();
        public List<SuppressOperation> SuppressOperation { get; } = SuppressOperationPool.Allocate();
        public List<AlignTokensOperation> AlignmentOperation { get; } = AlignTokensOperationPool.Allocate();
        public List<AnchorIndentationOperation> AnchorIndentationOperations { get; } = AnchorIndentationOperationPool.Allocate();

        public void Dispose()
        {
            IndentBlockOperation.Clear();
            IndentBlockOperationPool.Free(IndentBlockOperation);

            SuppressOperation.Clear();
            SuppressOperationPool.Free(SuppressOperation);

            AlignmentOperation.Clear();
            AlignTokensOperationPool.Free(AlignmentOperation);

            AnchorIndentationOperations.Clear();
            AnchorIndentationOperationPool.Free(AnchorIndentationOperations);
        }
    }
}
