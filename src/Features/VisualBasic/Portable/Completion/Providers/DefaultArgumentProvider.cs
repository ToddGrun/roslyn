// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportArgumentProvider(nameof(DefaultArgumentProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(ContextVariableArgumentProvider))]
[Shared]
internal class DefaultArgumentProvider : AbstractDefaultArgumentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultArgumentProvider()
    {
    }

    public override Task ProvideArgumentAsync(ArgumentContext context)
    {
        if (context.PreviousValue != null)
        {
            context.DefaultValue = context.PreviousValue;
        }
        else
        {
            context.DefaultValue = context.Parameter.Type.SpecialType switch
            {
                SpecialType.System_Boolean => "False",
                SpecialType.System_Char => "Chr(0)",
                SpecialType.System_Byte => "CByte(0)",
                SpecialType.System_SByte => "CSByte(0)",
                SpecialType.System_Int16 => "0S",
                SpecialType.System_UInt16 => "0US",
                SpecialType.System_Int32 => "0",
                SpecialType.System_UInt32 => "0U",
                SpecialType.System_Int64 => "0L",
                SpecialType.System_UInt64 => "0UL",
                SpecialType.System_Decimal => "0.0D",
                SpecialType.System_Single => "0.0F",
                SpecialType.System_Double => "0.0",
                _ => "Nothing"
            };
        }

        return Task.CompletedTask;
    }
}
