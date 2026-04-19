// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Zstd.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Filters.
//
// --[ Description ] ----------------------------------------------------------
//
//     Allow to open files that are compressed using Zstd.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Helpers.IO;
using SharpCompress.Providers.Default;

namespace Aaru.Filters;

/// <inheritdoc />
/// <summary>Decompress Zstd files while reading</summary>
public sealed class Zstd : IFilter
{
    Stream _dataStream;
    Stream _innerStream;

#region IFilter Members

    /// <inheritdoc />
    public string Name => "Zstd";

    /// <inheritdoc />
    public Guid Id => new("3BA80B07-66EE-4439-8EF5-3EB4D9D668F7");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public void Close()
    {
        _dataStream?.Close();
        _dataStream = null;
        BasePath    = null;
    }

    /// <inheritdoc />
    public string BasePath { get; private set; }

    /// <inheritdoc />
    public Stream GetDataForkStream() => _innerStream;

    /// <inheritdoc />
    public string Path => BasePath;

    /// <inheritdoc />
    public Stream GetResourceForkStream() => null;

    /// <inheritdoc />
    public bool HasResourceFork => false;

    /// <inheritdoc />
    public bool Identify(byte[] buffer) => buffer[0] == 0x28 &&
                                           buffer[1] == 0xB5 &&
                                           buffer[2] == 0x2F &&
                                           buffer[3] == 0xFD;

    /// <inheritdoc />
    public bool Identify(Stream stream)
    {
        var buffer = new byte[4];

        if(stream.Length < 4) return false;

        stream.Seek(0, SeekOrigin.Begin);
        stream.EnsureRead(buffer, 0, 4);
        stream.Seek(0, SeekOrigin.Begin);

        return buffer[0] == 0x28 && buffer[1] == 0xB5 && buffer[2] == 0x2F && buffer[3] == 0xFD;
    }

    /// <inheritdoc />
    public bool Identify(string path)
    {
        if(!File.Exists(path)) return false;

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var buffer = new byte[4];

        if(stream.Length < 8) return false;

        stream.Seek(0, SeekOrigin.Begin);
        stream.EnsureRead(buffer, 0, 4);
        stream.Seek(0, SeekOrigin.Begin);

        return buffer[0] == 0x28 && buffer[1] == 0xB5 && buffer[2] == 0x2F && buffer[3] == 0xFD;
    }

    /// <inheritdoc />
    public ErrorNumber Open(byte[] buffer)
    {
        _dataStream   = new MemoryStream(buffer);
        BasePath      = null;
        CreationTime  = DateTime.UtcNow;
        LastWriteTime = CreationTime;
        var provider = new ZStandardCompressionProvider();
        _innerStream = new ForcedSeekStream<Stream>(provider.CreateDecompressStream(_dataStream));

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Open(Stream stream)
    {
        _dataStream   = stream;
        BasePath      = null;
        CreationTime  = DateTime.UtcNow;
        LastWriteTime = CreationTime;
        var provider = new ZStandardCompressionProvider();
        _innerStream = new ForcedSeekStream<Stream>(provider.CreateDecompressStream(_dataStream));

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Open(string path)
    {
        _dataStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        BasePath    = System.IO.Path.GetFullPath(path);

        var fi = new FileInfo(path);
        CreationTime  = fi.CreationTimeUtc;
        LastWriteTime = fi.LastWriteTimeUtc;
        var provider = new ZStandardCompressionProvider();
        _innerStream = new ForcedSeekStream<Stream>(provider.CreateDecompressStream(_dataStream));

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public DateTime CreationTime { get; private set; }

    /// <inheritdoc />
    public long DataForkLength { get; private set; }

    /// <inheritdoc />
    public DateTime LastWriteTime { get; private set; }

    /// <inheritdoc />
    public long Length => DataForkLength;

    /// <inheritdoc />
    public long ResourceForkLength => 0;

    /// <inheritdoc />
    public string Filename
    {
        get
        {
            if(BasePath?.EndsWith(".zst", StringComparison.InvariantCultureIgnoreCase) == true) return BasePath[..^4];

            return BasePath?.EndsWith(".zstd", StringComparison.InvariantCultureIgnoreCase) == true
                       ? BasePath[..^5]
                       : BasePath;
        }
    }

    /// <inheritdoc />
    public string ParentFolder => System.IO.Path.GetDirectoryName(BasePath);

#endregion
}