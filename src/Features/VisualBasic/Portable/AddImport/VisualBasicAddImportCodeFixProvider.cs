// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.VisualBasic.AddImport;

internal static class AddImportDiagnosticIds
{
    /// <summary>
    /// Type xxx is not defined
    /// </summary>
    internal const string BC30002 = "BC30002";

    /// <summary>
    /// Error 'x' is not declared
    /// </summary>
    internal const string BC30451 = "BC30451";

    /// <summary>
    /// xxx is not a member of yyy
    /// </summary>
    internal const string BC30456 = "BC30456";

    /// <summary>
    /// 'X' has no parameters and its return type cannot be indexed
    /// </summary>
    internal const string BC32016 = "BC32016";

    /// <summary>
    /// Too few type arguments
    /// </summary>
    internal const string BC32042 = "BC32042";

    /// <summary>
    /// Expression of type xxx is not queryable
    /// </summary>
    internal const string BC36593 = "BC36593";

    /// <summary>
    /// 'A' has no type parameters and so cannot have type arguments.
    /// </summary>
    internal const string BC32045 = "BC32045";

    /// <summary>
    /// 'A' is not accessible in this context because it is 'Friend'.
    /// </summary>
    internal const string BC30389 = "BC30389";

    /// <summary>
    /// 'A' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
    /// </summary>
    internal const string BC31504 = "BC31504";

    /// <summary>
    /// Name 'A' is either not declared or not in the current scope.
    /// </summary>
    internal const string BC36610 = "BC36610";

    /// <summary>
    /// Cannot initialize the type 'A' with a collection initializer because it does not have an accessible 'Add' method
    /// </summary>
    internal const string BC36719 = "BC36719";

    /// <summary>
    /// Option Strict On disallows implicit conversions from 'Integer' to 'String'.
    /// </summary>
    internal const string BC30512 = "BC30512";

    /// <summary>
    /// 'A' is not accessible in this context because it is 'Private'.
    /// </summary>
    internal const string BC30390 = "BC30390";

    /// <summary>
    /// XML comment has a tag with a 'cref' attribute that could not be resolved. XML comment will be ignored.
    /// </summary>
    internal const string BC42309 = "BC42309";

    /// <summary>
    /// Type expected.
    /// </summary>
    internal const string BC30182 = "BC30182";

    /// <summary>
    /// 'A' should have suitable 'GetAwaiter' method.
    /// </summary>
    internal const string BC36930 = "BC36930";

    public static ImmutableArray<string> FixableDiagnosticIds { get; } =
        [BC30002, BC30451, BC30456, BC32042, BC36593, BC32045, BC30389, BC31504, BC32016, BC36610,
         BC36719, BC30512, BC30390, BC42309, BC30182, BC36930, IDEDiagnosticIds.UnboundIdentifierId];
}

[ExportCodeFixProvider(LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.AddImport), Shared]
internal class VisualBasicAddImportCodeFixProvider : AbstractAddImportCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public VisualBasicAddImportCodeFixProvider()
    {
    }

    /// <summary>
    /// For testing purposes so that tests can pass in mocks for these values.
    /// </summary>
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    internal VisualBasicAddImportCodeFixProvider(IPackageInstallerService installerService,
                   ISymbolSearchService searchService)
        : base(installerService, searchService)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;
}
