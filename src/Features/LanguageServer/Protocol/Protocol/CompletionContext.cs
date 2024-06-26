﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing additional information about the content in which a completion request is triggered.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionContext">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionContext
    {
        /// <summary>
        /// Gets or sets the <see cref="CompletionTriggerKind"/> indicating how the completion was triggered.
        /// </summary>
        [JsonPropertyName("triggerKind")]
        public CompletionTriggerKind TriggerKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the character that triggered code completion.
        /// </summary>
        [JsonPropertyName("triggerCharacter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TriggerCharacter
        {
            get;
            set;
        }
    }
}