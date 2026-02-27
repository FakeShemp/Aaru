// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory case
        if(normalizedPath == "/")
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new XfsDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "OpenDir: Traversing path with {0} components", pathComponents.Length);

        // Start from root directory cache
        Dictionary<string, ulong> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Navigating to component '{0}'", component);

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out ulong inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' found with inode {1}", component, inodeNumber);

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Dinode inode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading inode {0}: {1}", inodeNumber, errno);

                return errno;
            }

            // Check if it's a directory
            if((inode.di_mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: '{0}' is not a directory (mode=0x{1:X4})",
                                  component,
                                  inode.di_mode);

                return ErrorNumber.NotDirectory;
            }

            // Get or read directory contents
            errno = GetDirectoryContents(inodeNumber, inode, out Dictionary<string, ulong> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory contents: {0}", errno);

                return errno;
            }

            // If this is the last component, we're opening this directory
            if(p == pathComponents.Length - 1)
            {
                node = new XfsDirNode
                {
                    Path     = normalizedPath,
                    Position = 0,
                    Entries  = dirEntries.Keys.ToArray()
                };

                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Successfully opened directory '{0}' with {1} entries",
                                  normalizedPath,
                                  dirEntries.Count);

                return ErrorNumber.NoError;
            }

            // Not the last component — move to next level
            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not XfsDirNode xfsNode) return ErrorNumber.InvalidArgument;

        xfsNode.Position = -1;
        xfsNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not XfsDirNode xfsNode) return ErrorNumber.InvalidArgument;

        if(xfsNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(xfsNode.Position >= xfsNode.Entries.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = xfsNode.Entries[xfsNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Gets or reads directory contents for an inode, using the directory cache</summary>
    /// <param name="inodeNumber">Inode number of the directory</param>
    /// <param name="inode">The directory inode structure</param>
    /// <param name="entries">Output dictionary of entries (filename -> inode number), excluding . and ..</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetDirectoryContents(ulong inodeNumber, Dinode inode, out Dictionary<string, ulong> entries)
    {
        entries = new Dictionary<string, ulong>();

        ErrorNumber errno = inode.di_format switch
                            {
                                XFS_DINODE_FMT_LOCAL   => ReadShortformDirectory(inodeNumber, inode, entries),
                                XFS_DINODE_FMT_EXTENTS => ReadExtentDirectory(inodeNumber, inode, entries),
                                XFS_DINODE_FMT_BTREE   => ReadBtreeDirectory(inodeNumber, inode, entries),
                                _                      => ErrorNumber.NotSupported
                            };

        return errno;
    }

    /// <summary>Reads a shortform (inline) directory from the inode data fork</summary>
    /// <param name="inodeNumber">Inode number to read raw data from</param>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadShortformDirectory(ulong inodeNumber, Dinode inode, Dictionary<string, ulong> entries)
    {
        AaruLogging.Debug(MODULE_NAME, "Reading shortform directory for inode {0}", inodeNumber);

        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        if(rawInode.Length < coreSize + 6)
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for shortform directory");

            return ErrorNumber.InvalidArgument;
        }

        int pos = coreSize;

        byte count   = rawInode[pos];
        byte i8count = rawInode[pos + 1];

        bool use64BitInodes = i8count > 0;

        int parentInoSize = use64BitInodes ? 8 : 4;
        int headerSize    = 2 + parentInoSize;

        AaruLogging.Debug(MODULE_NAME, "SF dir: count={0}, i8count={1}, use64={2}", count, i8count, use64BitInodes);

        if(pos + headerSize > rawInode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for shortform header");

            return ErrorNumber.InvalidArgument;
        }

        pos += headerSize;

        int totalEntries = count;

        for(var i = 0; i < totalEntries; i++)
        {
            if(pos + 3 > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated shortform entry at index {0}", i);

                break;
            }

            byte nameLen = rawInode[pos];
            pos += 1 + 2; // skip namelen + offset

            if(pos + nameLen > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated name in shortform entry at index {0}", i);

                break;
            }

            string name = _encoding.GetString(rawInode, pos, nameLen);
            pos += nameLen;

            if(_hasFtype)
            {
                if(pos + 1 > rawInode.Length) break;

                pos += 1;
            }

            ulong entryIno;

            if(use64BitInodes)
            {
                if(pos + 8 > rawInode.Length) break;

                entryIno =  BigEndianBitConverter.ToUInt64(rawInode, pos) & XFS_MAXINUMBER;
                pos      += 8;
            }
            else
            {
                if(pos + 4 > rawInode.Length) break;

                entryIno =  BigEndianBitConverter.ToUInt32(rawInode, pos);
                pos      += 4;
            }

            if(name is "." or "..") continue;

            entries[name] = entryIno;

            AaruLogging.Debug(MODULE_NAME, "SF entry: \"{0}\" -> inode {1}", name, entryIno);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a directory stored in extent format</summary>
    /// <param name="inodeNumber">Inode number to read raw data from</param>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadExtentDirectory(ulong inodeNumber, Dinode inode, Dictionary<string, ulong> entries)
    {
        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        // Determine extent count based on NREXT64 feature (per-inode flag in di_flags2)
        ulong extentCount;

        if(_v3Inodes && (inode.di_flags2 & XFS_DIFLAG2_NREXT64) != 0)
        {
            // di_big_nextents is at offset 24 (di_v2_pad/di_flushiter position) as 8 bytes for V3 NREXT64
            if(rawInode.Length < 32)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated inode data for NREXT64");

                return ErrorNumber.InvalidArgument;
            }

            extentCount = BigEndianBitConverter.ToUInt64(rawInode, 24);

            AaruLogging.Debug(MODULE_NAME,
                              "Reading extent directory for inode {0} ({1} extents, NREXT64)",
                              inodeNumber,
                              extentCount);
        }
        else
        {
            extentCount = inode.di_nextents;

            AaruLogging.Debug(MODULE_NAME,
                              "Reading extent directory for inode {0} ({1} extents)",
                              inodeNumber,
                              extentCount);
        }

        int pos = coreSize;

        // Directory blocks may span multiple filesystem blocks (1 << dirblklog)
        uint dirBlockFsBlocks = 1U << _superblock.dirblklog;

        for(ulong i = 0; i < extentCount; i++)
        {
            if(pos + 16 > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated extent record at index {0}", i);

                break;
            }

            var l0 = BigEndianBitConverter.ToUInt64(rawInode, pos);
            var l1 = BigEndianBitConverter.ToUInt64(rawInode, pos + 8);
            pos += 16;

            DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount, out _);

            AaruLogging.Debug(MODULE_NAME, "Extent {0}: startblock={1}, count={2}", i, startBlock, blockCount);

            for(uint b = 0; b < blockCount; b += dirBlockFsBlocks)
            {
                uint fsBlocksToRead = Math.Min(dirBlockFsBlocks, blockCount - b);

                // Read and assemble the full directory block
                var dirBlockData = new byte[fsBlocksToRead * _superblock.blocksize];
                var validRead    = true;

                for(uint fb = 0; fb < fsBlocksToRead; fb++)
                {
                    errno = ReadBlock(startBlock + b + fb, out byte[] blockData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "Error reading directory block {0}: {1}",
                                          startBlock + b + fb,
                                          errno);

                        validRead = false;

                        break;
                    }

                    Array.Copy(blockData, 0, dirBlockData, fb * _superblock.blocksize, blockData.Length);
                }

                if(validRead) ParseDirectoryDataBlock(dirBlockData, entries);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a directory stored in btree format</summary>
    /// <param name="inodeNumber">Inode number to read raw data from</param>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBtreeDirectory(ulong inodeNumber, Dinode inode, Dictionary<string, ulong> entries)
    {
        AaruLogging.Debug(MODULE_NAME, "Reading btree directory for inode {0}", inodeNumber);

        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int pos = coreSize;

        if(pos + 4 > rawInode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for bmdr_block header");

            return ErrorNumber.InvalidArgument;
        }

        var level   = BigEndianBitConverter.ToUInt16(rawInode, pos);
        var numrecs = BigEndianBitConverter.ToUInt16(rawInode, pos + 2);

        AaruLogging.Debug(MODULE_NAME, "BMDR: level={0}, numrecs={1}", level, numrecs);

        if(level == 0)
        {
            int recPos = pos + 4;

            for(var i = 0; i < numrecs; i++)
            {
                if(recPos + 16 > rawInode.Length) break;

                var l0 = BigEndianBitConverter.ToUInt64(rawInode, recPos);
                var l1 = BigEndianBitConverter.ToUInt64(rawInode, recPos + 8);
                recPos += 16;

                DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount, out _);

                uint dirBlockFsBlocks = 1U << _superblock.dirblklog;

                for(uint b = 0; b < blockCount; b += dirBlockFsBlocks)
                {
                    uint fsBlocksToRead = Math.Min(dirBlockFsBlocks, blockCount - b);

                    var dirBlockData = new byte[fsBlocksToRead * _superblock.blocksize];
                    var validRead    = true;

                    for(uint fb = 0; fb < fsBlocksToRead; fb++)
                    {
                        errno = ReadBlock(startBlock + b + fb, out byte[] blockData);

                        if(errno != ErrorNumber.NoError)
                        {
                            validRead = false;

                            break;
                        }

                        Array.Copy(blockData, 0, dirBlockData, fb * _superblock.blocksize, blockData.Length);
                    }

                    if(validRead) ParseDirectoryDataBlock(dirBlockData, entries);
                }
            }
        }
        else
        {
            int forkSize = inode.di_forkoff > 0 ? inode.di_forkoff * 8 : rawInode.Length - coreSize;

            // maxrecs = max records that fit in the BMDR block
            // Layout: header(4) + keys(maxrecs*8) + ptrs(maxrecs*8)
            int maxrecs   = (forkSize - 4) / 16;
            int ptrsStart = pos + 4 + maxrecs * 8;

            for(var i = 0; i < numrecs; i++)
            {
                int ptrPos = ptrsStart + i * 8;

                if(ptrPos + 8 > rawInode.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(rawInode, ptrPos);

                errno = ReadBmapBtreeBlock(childBlock, level - 1, entries);

                if(errno != ErrorNumber.NoError)
                    AaruLogging.Debug(MODULE_NAME, "Error reading bmap btree child block {0}: {1}", childBlock, errno);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a directory data block (v2 or v3 format) and adds entries to the given dictionary</summary>
    /// <param name="blockData">The raw block data</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    void ParseDirectoryDataBlock(byte[] blockData, Dictionary<string, ulong> entries)
    {
        if(blockData.Length < 16) return;

        var magic = BigEndianBitConverter.ToUInt32(blockData, 0);

        int dataStart;

        switch(magic)
        {
            case XFS_DIR2_BLOCK_MAGIC:
            case XFS_DIR2_DATA_MAGIC:
                dataStart = 16;

                break;
            case XFS_DIR3_BLOCK_MAGIC:
            case XFS_DIR3_DATA_MAGIC:
                dataStart = 64;

                break;
            default:
                AaruLogging.Debug(MODULE_NAME, "Unknown directory block magic: 0x{0:X8}", magic);

                return;
        }

        int pos = dataStart;

        while(pos + 10 < blockData.Length)
        {
            var freetag = BigEndianBitConverter.ToUInt16(blockData, pos);

            if(freetag == 0xFFFF)
            {
                if(pos + 4 > blockData.Length) break;

                var freeLen = BigEndianBitConverter.ToUInt16(blockData, pos + 2);

                if(freeLen == 0 || pos + freeLen > blockData.Length) break;

                pos += freeLen;

                continue;
            }

            if(pos + 9 > blockData.Length) break;

            var  entryIno = BigEndianBitConverter.ToUInt64(blockData, pos);
            byte nameLen  = blockData[pos + 8];

            if(nameLen == 0 || pos + 9 + nameLen > blockData.Length) break;

            string name = _encoding.GetString(blockData, pos + 9, nameLen);

            int afterName = pos + 9 + nameLen;

            if(_hasFtype) afterName++;

            int entryEnd = (afterName + 2 + 7) / 8 * 8;

            if(name is not "." and not "..")
            {
                entries[name] = entryIno;

                AaruLogging.Debug(MODULE_NAME, "Block entry: \"{0}\" -> inode {1}", name, entryIno);
            }

            pos = entryEnd;
        }
    }
}