﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed partial class SymbolSearchUpdateEngine
{
    private sealed class IOService : IIOService
    {
        public void Create(DirectoryInfo directory) => directory.Create();

        public void Delete(FileInfo file) => file.Delete();

        public bool Exists(FileSystemInfo info) => info.Exists;

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors)
            => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

        public void Move(string sourceFileName, string destinationFileName)
            => File.Move(sourceFileName, destinationFileName);

        public void WriteAndFlushAllBytes(string path, ArraySegment<byte> bytes)
        {
            using var fileStream = new FileStream(path, FileMode.Create);

            fileStream.Write(bytes.Array!, bytes.Offset, bytes.Count);
            fileStream.Flush(flushToDisk: true);
        }
    }
}
