// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <summary>Reads the root inode and returns its stat information</summary>
    /// <param name="stat">Output: File entry info for the root directory</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadRootInodeStat(out FileEntryInfo stat)
    {
        stat = null;

        // Extract root inode block and offset from root_inode reference
        var rootInodeBlock  = (uint)(_superBlock.root_inode >> 16);
        var rootInodeOffset = (uint)(_superBlock.root_inode & 0xFFFF);

        return ReadInodeStat(rootInodeBlock, (ushort)rootInodeOffset, out stat);
    }

    /// <summary>Reads a directory inode and extracts directory parameters</summary>
    /// <param name="inodeBlock">Block containing the inode (relative to inode table)</param>
    /// <param name="inodeOffset">Offset within the metadata block</param>
    /// <param name="startBlock">Output: start block of directory data</param>
    /// <param name="offset">Output: offset within the directory block</param>
    /// <param name="size">Output: size of directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryInode(uint     inodeBlock, ushort inodeOffset, out uint startBlock, out uint offset,
                                   out uint size)
    {
        startBlock = 0;
        offset     = 0;
        size       = 0;

        // Calculate absolute position of the inode
        ulong inodePosition = _superBlock.inode_table_start + inodeBlock;

        // Read the metadata block containing the inode
        ErrorNumber errno = ReadMetadataBlock(inodePosition, out byte[] inodeBlockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode metadata block: {0}", errno);

            return errno;
        }

        if(inodeBlockData == null || inodeBlockData.Length <= inodeOffset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode block data");

            return ErrorNumber.InvalidArgument;
        }

        // Read the base inode to get the type
        var baseInodeData = new byte[Marshal.SizeOf<BaseInode>()];
        Array.Copy(inodeBlockData, inodeOffset, baseInodeData, 0, baseInodeData.Length);

        BaseInode baseInode = _littleEndian
                                  ? Helpers.Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Helpers.Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        // Read the directory inode based on type
        if(baseInode.inode_type == (ushort)SquashInodeType.Directory)
        {
            var dirInodeData = new byte[Marshal.SizeOf<DirInode>()];
            Array.Copy(inodeBlockData, inodeOffset, dirInodeData, 0, dirInodeData.Length);

            DirInode dirInode = _littleEndian
                                    ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirInode>(dirInodeData)
                                    : Helpers.Marshal.ByteArrayToStructureBigEndian<DirInode>(dirInodeData);

            startBlock = dirInode.start_block;
            size       = dirInode.file_size;
            offset     = dirInode.offset;
        }
        else if(baseInode.inode_type == (ushort)SquashInodeType.ExtendedDirectory)
        {
            var extDirInodeData = new byte[Marshal.SizeOf<ExtendedDirInode>()];
            Array.Copy(inodeBlockData, inodeOffset, extDirInodeData, 0, extDirInodeData.Length);

            ExtendedDirInode extDirInode = _littleEndian
                                               ? Helpers.Marshal
                                                        .ByteArrayToStructureLittleEndian<
                                                             ExtendedDirInode>(extDirInodeData)
                                               : Helpers.Marshal
                                                        .ByteArrayToStructureBigEndian<
                                                             ExtendedDirInode>(extDirInodeData);

            startBlock = extDirInode.start_block;
            size       = extDirInode.file_size;
            offset     = extDirInode.offset;
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Inode is not a directory, type: {0}",
                              (SquashInodeType)baseInode.inode_type);

            return ErrorNumber.NotDirectory;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Directory inode: start_block={0}, size={1}, offset={2}",
                          startBlock,
                          size,
                          offset);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an inode and returns its stat information</summary>
    /// <param name="inodeBlock">Block containing the inode (relative to inode table)</param>
    /// <param name="inodeOffset">Offset within the metadata block</param>
    /// <param name="stat">Output: File entry info</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeStat(uint inodeBlock, ushort inodeOffset, out FileEntryInfo stat)
    {
        stat = null;

        // Calculate absolute position of the inode
        ulong inodePosition = _superBlock.inode_table_start + inodeBlock;

        // Read the metadata block containing the inode
        ErrorNumber errno = ReadMetadataBlock(inodePosition, out byte[] inodeBlockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode metadata block: {0}", errno);

            return errno;
        }

        if(inodeBlockData == null || inodeBlockData.Length <= inodeOffset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode block data");

            return ErrorNumber.InvalidArgument;
        }

        // Read the base inode to get the type
        var baseInodeData = new byte[Marshal.SizeOf<BaseInode>()];
        Array.Copy(inodeBlockData, inodeOffset, baseInodeData, 0, baseInodeData.Length);

        BaseInode baseInode = _littleEndian
                                  ? Helpers.Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Helpers.Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        // Create file entry info based on inode type
        stat = new FileEntryInfo
        {
            Inode            = baseInode.inode_number,
            Mode             = baseInode.mode,
            UID              = baseInode.uid,
            GID              = baseInode.guid,
            LastWriteTimeUtc = DateHandlers.UnixUnsignedToDateTime(baseInode.mtime),
            BlockSize        = _superBlock.block_size,
            Attributes       = FileAttributes.None
        };

        // Get additional info based on inode type
        var inodeType = (SquashInodeType)baseInode.inode_type;

        switch(inodeType)
        {
            case SquashInodeType.Directory:
            {
                var dirInodeData = new byte[Marshal.SizeOf<DirInode>()];
                Array.Copy(inodeBlockData, inodeOffset, dirInodeData, 0, dirInodeData.Length);

                DirInode dirInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirInode>(dirInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<DirInode>(dirInodeData);

                stat.Attributes = FileAttributes.Directory;
                stat.Length     = dirInode.file_size;
                stat.Links      = dirInode.nlink;

                break;
            }

            case SquashInodeType.ExtendedDirectory:
            {
                var extDirInodeData = new byte[Marshal.SizeOf<ExtendedDirInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extDirInodeData, 0, extDirInodeData.Length);

                ExtendedDirInode extDirInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedDirInode>(extDirInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedDirInode>(extDirInodeData);

                stat.Attributes = FileAttributes.Directory;
                stat.Length     = extDirInode.file_size;
                stat.Links      = extDirInode.nlink;

                break;
            }

            case SquashInodeType.RegularFile:
            {
                var regInodeData = new byte[Marshal.SizeOf<RegInode>()];
                Array.Copy(inodeBlockData, inodeOffset, regInodeData, 0, regInodeData.Length);

                RegInode regInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<RegInode>(regInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<RegInode>(regInodeData);

                stat.Attributes = FileAttributes.File;
                stat.Length     = regInode.file_size;
                stat.Blocks     = (regInode.file_size + _superBlock.block_size - 1) / _superBlock.block_size;

                break;
            }

            case SquashInodeType.ExtendedRegularFile:
            {
                var extRegInodeData = new byte[Marshal.SizeOf<ExtendedRegInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extRegInodeData, 0, extRegInodeData.Length);

                ExtendedRegInode extRegInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedRegInode>(extRegInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedRegInode>(extRegInodeData);

                stat.Attributes = FileAttributes.File;
                stat.Length     = (long)extRegInode.file_size;
                stat.Links      = extRegInode.nlink;
                stat.Blocks     = (long)((extRegInode.file_size + _superBlock.block_size - 1) / _superBlock.block_size);

                break;
            }

            case SquashInodeType.Symlink:
            case SquashInodeType.ExtendedSymlink:
            {
                var symlinkInodeData = new byte[Marshal.SizeOf<SymlinkInode>()];
                Array.Copy(inodeBlockData, inodeOffset, symlinkInodeData, 0, symlinkInodeData.Length);

                SymlinkInode symlinkInode = _littleEndian
                                                ? Helpers.Marshal
                                                         .ByteArrayToStructureLittleEndian<
                                                              SymlinkInode>(symlinkInodeData)
                                                : Helpers.Marshal
                                                         .ByteArrayToStructureBigEndian<SymlinkInode>(symlinkInodeData);

                stat.Attributes = FileAttributes.Symlink;
                stat.Length     = symlinkInode.symlink_size;
                stat.Links      = symlinkInode.nlink;

                break;
            }

            case SquashInodeType.BlockDevice:
            case SquashInodeType.ExtendedBlockDevice:
            {
                var devInodeData = new byte[Marshal.SizeOf<DevInode>()];
                Array.Copy(inodeBlockData, inodeOffset, devInodeData, 0, devInodeData.Length);

                DevInode devInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DevInode>(devInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<DevInode>(devInodeData);

                stat.Attributes = FileAttributes.BlockDevice;
                stat.Links      = devInode.nlink;

                // Device number: major in upper 12 bits, minor in lower 20 bits (Linux encoding)
                uint major = devInode.rdev >> 8 & 0xFFF;
                uint minor = devInode.rdev & 0xFF | devInode.rdev >> 12 & 0xFFF00;
                stat.DeviceNo = (ulong)major << 32 | minor;

                break;
            }

            case SquashInodeType.CharacterDevice:
            case SquashInodeType.ExtendedCharDevice:
            {
                var devInodeData = new byte[Marshal.SizeOf<DevInode>()];
                Array.Copy(inodeBlockData, inodeOffset, devInodeData, 0, devInodeData.Length);

                DevInode devInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DevInode>(devInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<DevInode>(devInodeData);

                stat.Attributes = FileAttributes.CharDevice;
                stat.Links      = devInode.nlink;

                // Device number: major in upper 12 bits, minor in lower 20 bits (Linux encoding)
                uint major = devInode.rdev >> 8 & 0xFFF;
                uint minor = devInode.rdev & 0xFF | devInode.rdev >> 12 & 0xFFF00;
                stat.DeviceNo = (ulong)major << 32 | minor;

                break;
            }

            case SquashInodeType.Fifo:
            case SquashInodeType.ExtendedFifo:
            {
                var ipcInodeData = new byte[Marshal.SizeOf<IpcInode>()];
                Array.Copy(inodeBlockData, inodeOffset, ipcInodeData, 0, ipcInodeData.Length);

                IpcInode ipcInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<IpcInode>(ipcInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<IpcInode>(ipcInodeData);

                stat.Attributes = FileAttributes.FIFO;
                stat.Links      = ipcInode.nlink;

                break;
            }

            case SquashInodeType.Socket:
            case SquashInodeType.ExtendedSocket:
            {
                var ipcInodeData = new byte[Marshal.SizeOf<IpcInode>()];
                Array.Copy(inodeBlockData, inodeOffset, ipcInodeData, 0, ipcInodeData.Length);

                IpcInode ipcInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<IpcInode>(ipcInodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<IpcInode>(ipcInodeData);

                stat.Attributes = FileAttributes.Socket;
                stat.Links      = ipcInode.nlink;

                break;
            }

            default:
                AaruLogging.Debug(MODULE_NAME, "Unknown inode type: {0}", inodeType);
                stat.Attributes = FileAttributes.File;

                break;
        }

        return ErrorNumber.NoError;
    }
}