﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A <see cref="Document"/> that was generated by an <see cref="ISourceGenerator" />.
/// </summary>
public sealed class SourceGeneratedDocument : Document
{
    internal SourceGeneratedDocument(Project project, SourceGeneratedDocumentState state)
        : base(project, state)
    {
    }

    private new SourceGeneratedDocumentState State => (SourceGeneratedDocumentState)base.State;

    public string HintName => State.HintName;

    // TODO: make this public. Tracked by https://github.com/dotnet/roslyn/issues/50546
    internal SourceGeneratedDocumentIdentity Identity => State.Identity;

    internal DateTime GenerationDateTime => State.GenerationDateTime;

    internal new SourceGeneratedDocument WithText(SourceText text)
        => (SourceGeneratedDocument)base.WithText(text);

    internal new SourceGeneratedDocument WithSyntaxRoot(SyntaxNode root)
        => (SourceGeneratedDocument)base.WithSyntaxRoot(root);

    internal override Document WithFrozenPartialSemantics(bool forceFreeze, CancellationToken cancellationToken)
    {
        // For us to implement frozen partial semantics here with a source generated document,
        // we'd need to potentially deal with the combination where that happens on a snapshot that was already
        // forked; rather than trying to deal with that combo we'll just fall back to not doing anything special
        // which is allowed.
        return this;
    }
}
