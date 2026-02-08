// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
#region Nested type: SquashCompression

    /// <summary>Compression algorithms supported by squashfs</summary>
    enum SquashCompression : ushort
    {
        /// <summary>zlib compression</summary>
        Zlib = 1,
        /// <summary>LZMA compression</summary>
        Lzma = 2,
        /// <summary>LZO compression</summary>
        Lzo = 3,
        /// <summary>XZ compression</summary>
        Xz = 4,
        /// <summary>LZ4 compression</summary>
        Lz4 = 5,
        /// <summary>Zstandard compression</summary>
        Zstd = 6
    }

#endregion

#region Nested type: SquashInodeType

    /// <summary>Inode types including extended types</summary>
    enum SquashInodeType : ushort
    {
        /// <summary>Directory</summary>
        Directory = 1,
        /// <summary>Regular file</summary>
        RegularFile = 2,
        /// <summary>Symbolic link</summary>
        Symlink = 3,
        /// <summary>Block device</summary>
        BlockDevice = 4,
        /// <summary>Character device</summary>
        CharacterDevice = 5,
        /// <summary>FIFO (named pipe)</summary>
        Fifo = 6,
        /// <summary>Socket</summary>
        Socket = 7,
        /// <summary>Extended directory</summary>
        ExtendedDirectory = 8,
        /// <summary>Extended regular file</summary>
        ExtendedRegularFile = 9,
        /// <summary>Extended symbolic link</summary>
        ExtendedSymlink = 10,
        /// <summary>Extended block device</summary>
        ExtendedBlockDevice = 11,
        /// <summary>Extended character device</summary>
        ExtendedCharDevice = 12,
        /// <summary>Extended FIFO (named pipe)</summary>
        ExtendedFifo = 13,
        /// <summary>Extended socket</summary>
        ExtendedSocket = 14
    }

#endregion

#region Nested type: SquashFlags

    /// <summary>Filesystem flags</summary>
    [Flags]
    enum SquashFlags : ushort
    {
        /// <summary>No flags set</summary>
        None = 0,
        /// <summary>Inodes are uncompressed</summary>
        UncompressedInodes = 1 << 0,
        /// <summary>Data blocks are uncompressed</summary>
        UncompressedData = 1 << 1,
        /// <summary>Fragments are uncompressed</summary>
        UncompressedFragments = 1 << 3,
        /// <summary>Fragments are not used</summary>
        NoFragments = 1 << 4,
        /// <summary>Always use fragments</summary>
        AlwaysFragments = 1 << 5,
        /// <summary>Duplicate removal was performed</summary>
        Duplicates = 1 << 6,
        /// <summary>Filesystem is exportable (NFS)</summary>
        Exportable = 1 << 7,
        /// <summary>Compressor-specific options are present</summary>
        CompressorOptions = 1 << 10
    }

#endregion

#region Nested type: SquashXattrType

    /// <summary>Extended attribute types</summary>
    [Flags]
    enum SquashXattrType : ushort
    {
        /// <summary>User namespace</summary>
        User = 0,
        /// <summary>Trusted namespace</summary>
        Trusted = 1,
        /// <summary>Security namespace</summary>
        Security = 2,
        /// <summary>Value stored out-of-line</summary>
        ValueOutOfLine = 256,
        /// <summary>Mask for xattr prefix</summary>
        PrefixMask = 0xFF
    }

#endregion
}