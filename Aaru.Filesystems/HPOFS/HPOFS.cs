// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HPOFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
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
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from test floppy images created with OS/2 HPOFS 2.0
// Need to get IBM document GA32-0224 -> IBM 3995 Optical Library Dataserver Products: Optical Disk Format
/// <inheritdoc />
/// <summary>Implements IBM's High Performance Optical File System</summary>
public sealed partial class HPOFS : IReadOnlyFilesystem
{
    const string       MODULE_NAME = "HPOFS Plugin";
    BiosParameterBlock _bpb;
    DciRecord          _dciRecord;

    bool                                                         _debug;
    Dictionary<string, Dictionary<string, CachedDirectoryEntry>> _directoryCache;
    Encoding                                                     _encoding;
    bool                                                         _hasSmiBlock;
    IMediaImage                                                  _image;
    MediaInformationBlock                                        _medInfo;
    bool                                                         _mounted;
    Partition                                                    _partition;
    Dictionary<string, CachedDirectoryEntry>                     _rootDirectoryCache;
    SmiBlock                                                     _smiBlock;
    FileSystemInfo                                               _statfs;
    VolumeInformationBlock                                       _volInfo;

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];

#region Nested type: CachedDirectoryEntry

    /// <summary>Cached directory entry for mounted filesystem operations</summary>
    sealed class CachedDirectoryEntry
    {
        /// <summary>DOS-style attribute byte from the directory entry</summary>
        public byte Attributes;
        /// <summary>Creation timestamp (Unix epoch, from record +0x1C)</summary>
        public uint CreationTimestamp;
        /// <summary>File size in bytes (from record +0x4C)</summary>
        public uint FileSize;
        /// <summary>Is this entry a directory?</summary>
        public bool IsDirectory;
        /// <summary>Modification timestamp (Unix epoch, from record +0x20)</summary>
        public uint ModificationTimestamp;
        /// <summary>Filename (last path component)</summary>
        public string Name;
        /// <summary>Sector address this entry points to</summary>
        public uint SectorAddress;
        /// <summary>Timestamp or checksum value from the INDX entry</summary>
        public uint Timestamp;
    }

#endregion

#region Nested type: HpofsDirNode

    sealed class HpofsDirNode : IDirNode
    {
        internal string[]                                 Contents;
        internal Dictionary<string, CachedDirectoryEntry> Entries;
        internal int                                      Position;
        public   string                                   Path { get; init; }
    }

#endregion

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.HPOFS_Name;

    /// <inheritdoc />
    public Guid Id => new("1b72dcd5-d031-4757-8a9f-8d2fb18c59e2");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}