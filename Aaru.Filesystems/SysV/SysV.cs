// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SysV.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from the Linux kernel, Coherent source code, XENIX includes, System V includes, 32V includes,
// OpenServer includes, and many other operating systems include headers
/// <inheritdoc />
/// <summary>Implements the UNIX System V filesystem</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class SysVfs : IReadOnlyFilesystem
{
    const string MODULE_NAME = "SysVfs";

    /// <summary>Identifies which byte sex the filesystem uses</summary>
    enum Bytesex
    {
        /// <summary>Little-endian</summary>
        LittleEndian,
        /// <summary>Big-endian</summary>
        BigEndian,
        /// <summary>PDP-11 middle-endian</summary>
        Pdp
    }

    /// <summary>Identifies which filesystem variant is mounted</summary>
    enum SysVVariant
    {
        /// <summary>XENIX filesystem (magic at 0x3F8)</summary>
        Xenix,
        /// <summary>XENIX 3 filesystem (magic at 0x1F0)</summary>
        Xenix3,
        /// <summary>System V Release 4</summary>
        SystemVR4,
        /// <summary>System V Release 2</summary>
        SystemVR2,
        /// <summary>SCO Acer File System</summary>
        ScoAfs,
        /// <summary>Coherent UNIX</summary>
        Coherent,
        /// <summary>UNIX 7th Edition</summary>
        UnixV7
    }

    /// <summary>Root inode number (1-based)</summary>
    const int SYSV_ROOT_INO = 2;

    /// <summary>Size of an on-disk inode in bytes</summary>
    const int INODE_SIZE = 64;

    /// <summary>First block containing inodes (always block 2)</summary>
    const int FIRST_INODE_ZONE = 2;

    /// <summary>S_IFMT mask for inode mode</summary>
    const ushort S_IFMT = 0xF000;

    /// <summary>S_IFDIR value for directory</summary>
    const ushort S_IFDIR = 0x4000;

    /// <summary>Cached root directory entries (filename -> inode number)</summary>
    readonly Dictionary<string, ushort> _rootDirectoryCache = new();

    /// <summary>Block size in bytes</summary>
    int _blockSize;

    /// <summary>Byte sex of the filesystem</summary>
    Bytesex _bytesex;

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>First data zone (from s_isize)</summary>
    int _firstDataZone;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Calculated inodes per block</summary>
    int _inodesPerBlock;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Sector offset where the superblock was found</summary>
    int _superblockStart;

    /// <summary>Byte offset within the superblock sector (for SysV when magic is at 0x3F8)</summary>
    int _superblockOffset;

    /// <summary>Which filesystem variant is mounted</summary>
    SysVVariant _variant;

    /// <summary>Total number of zones (blocks) in the filesystem</summary>
    long _totalZones;

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.SysVfs_Name;

    /// <inheritdoc />
    public Guid Id => new("9B8D016A-8561-400E-A12A-A198283C211D");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];
}