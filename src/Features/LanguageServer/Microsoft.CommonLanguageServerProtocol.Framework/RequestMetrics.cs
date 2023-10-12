// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CommonLanguageServerProtocol.Framework
{
    internal abstract class RequestMetrics
    {
        protected readonly string _methodName;

        public RequestMetrics(string methodName)
        {
            _methodName = methodName;
        }

        public abstract void RecordExecutionStart();
        public abstract void RecordSuccess();
        public abstract void RecordFailure();
        public abstract void RecordCancellation();
    }
}
