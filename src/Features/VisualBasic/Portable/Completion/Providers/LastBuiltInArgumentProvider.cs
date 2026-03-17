// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

/// <summary>
/// Provides an argument provider that always appears after all built-in argument providers. This argument
/// provider does not provide any argument values.
/// </summary>
[ExportArgumentProvider(nameof(LastBuiltInArgumentProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(DefaultArgumentProvider))]
[Shared]
internal class LastBuiltInArgumentProvider : ArgumentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LastBuiltInArgumentProvider()
    {
    }

    public override Task ProvideArgumentAsync(ArgumentContext context)
    {
        return Task.CompletedTask;
    }
}
