// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public interface ILspLoggerScope : IDisposable
{
    void AddProperty(string name, object? value);
    void AddProperties(ImmutableArray<KeyValuePair<string, object?>> properties);

    void AddException(Exception exception, string? message = null, params object[] @params);
    void AddWarning(string message, params object[] @params);
}
