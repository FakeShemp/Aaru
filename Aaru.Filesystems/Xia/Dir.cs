// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new XiaDirNode
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

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "Traversing path with {0} components", pathComponents.Length);

        // Start from root directory
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        foreach(string component in pathComponents)
        {
            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint childInodeNum))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the child inode
            ErrorNumber errno = ReadInode(childInodeNum, out Inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child inode: {0}", errno);

                return errno;
            }

            // Check if it's a directory (S_IFDIR = 0x4000)
            if((childInode.i_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory entries from child inode
            errno = ReadDirectoryEntries(childInode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child directory entries: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Create directory node with found entries
        node = new XiaDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = currentEntries.Keys.ToArray()
        };

        AaruLogging.Debug(MODULE_NAME,
                          "Successfully opened directory '{0}' with {1} entries",
                          normalizedPath,
                          currentEntries.Count);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not XiaDirNode xiaDirNode) return ErrorNumber.InvalidArgument;

        xiaDirNode.Position = -1;
        xiaDirNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not XiaDirNode xiaDirNode) return ErrorNumber.InvalidArgument;

        if(xiaDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(xiaDirNode.Position >= xiaDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = xiaDirNode.Entries[xiaDirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from an inode's data zones</summary>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(in Inode inode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        if(inode.i_size == 0) return ErrorNumber.NoError;

        // Read data from the inode's zone pointers
        // The inode has 8 direct zone pointers, 1 indirect, and 1 double indirect
        uint bytesRead        = 0;
        uint addressesPerZone = _superblock.s_zone_size / 4; // 4 bytes per zone pointer

        // Process direct zones (first 8 zone pointers)
        for(var i = 0; i < 8 && bytesRead < inode.i_size; i++)
        {
            // Zone addresses are stored in 24 bits (lowest 3 bytes)
            uint zoneAddr = inode.i_zone[i] & 0x00FFFFFF;

            if(zoneAddr == 0) continue;

            AaruLogging.Debug(MODULE_NAME, "Reading direct zone {0} at address {1}", i, zoneAddr);

            ErrorNumber errno = ReadZone(zoneAddr, out byte[] zoneData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading zone {0}: {1}", zoneAddr, errno);

                continue;
            }

            uint bytesToProcess = Math.Min(_superblock.s_zone_size, inode.i_size - bytesRead);
            ParseDirectoryZone(zoneData, bytesToProcess, entries);
            bytesRead += _superblock.s_zone_size;
        }

        // Process indirect zone (zone pointer 8)
        if(bytesRead < inode.i_size)
        {
            uint indirectZone = inode.i_zone[8] & 0x00FFFFFF;

            if(indirectZone != 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Reading indirect zone at address {0}", indirectZone);

                ErrorNumber errno = ReadZone(indirectZone, out byte[] indirectData);

                if(errno == ErrorNumber.NoError)
                {
                    for(uint i = 0; i < addressesPerZone && bytesRead < inode.i_size; i++)
                    {
                        uint zoneAddr = BitConverter.ToUInt32(indirectData, (int)(i * 4)) & 0x00FFFFFF;

                        if(zoneAddr == 0) continue;

                        errno = ReadZone(zoneAddr, out byte[] zoneData);

                        if(errno != ErrorNumber.NoError) continue;

                        uint bytesToProcess = Math.Min(_superblock.s_zone_size, inode.i_size - bytesRead);
                        ParseDirectoryZone(zoneData, bytesToProcess, entries);
                        bytesRead += _superblock.s_zone_size;
                    }
                }
            }
        }

        // Process double indirect zone (zone pointer 9)
        if(bytesRead < inode.i_size)
        {
            uint dindirectZone = inode.i_zone[9] & 0x00FFFFFF;

            if(dindirectZone != 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Reading double indirect zone at address {0}", dindirectZone);

                ErrorNumber errno = ReadZone(dindirectZone, out byte[] dindirectData);

                if(errno == ErrorNumber.NoError)
                {
                    for(uint i = 0; i < addressesPerZone && bytesRead < inode.i_size; i++)
                    {
                        uint indirectAddr = BitConverter.ToUInt32(dindirectData, (int)(i * 4)) & 0x00FFFFFF;

                        if(indirectAddr == 0) continue;

                        errno = ReadZone(indirectAddr, out byte[] indirectData);

                        if(errno != ErrorNumber.NoError) continue;

                        for(uint j = 0; j < addressesPerZone && bytesRead < inode.i_size; j++)
                        {
                            uint zoneAddr = BitConverter.ToUInt32(indirectData, (int)(j * 4)) & 0x00FFFFFF;

                            if(zoneAddr == 0) continue;

                            errno = ReadZone(zoneAddr, out byte[] zoneData);

                            if(errno != ErrorNumber.NoError) continue;

                            uint bytesToProcess = Math.Min(_superblock.s_zone_size, inode.i_size - bytesRead);
                            ParseDirectoryZone(zoneData, bytesToProcess, entries);
                            bytesRead += _superblock.s_zone_size;
                        }
                    }
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory entries from a zone's data</summary>
    /// <param name="zoneData">The zone data</param>
    /// <param name="validBytes">Number of valid bytes in the zone</param>
    /// <param name="entries">Dictionary to add entries to</param>
    void ParseDirectoryZone(byte[] zoneData, uint validBytes, Dictionary<string, uint> entries)
    {
        uint offset = 0;

        while(offset < validBytes)
        {
            // Directory entry structure:
            // - 4 bytes: inode number (d_ino)
            // - 2 bytes: record length (d_rec_len)
            // - 1 byte:  name length (d_name_len)
            // - N bytes: name (null-terminated, max 248+1 bytes)

            if(offset + 7 > validBytes) // Minimum entry size
                break;

            var  inoNum  = BitConverter.ToUInt32(zoneData, (int)offset);
            var  recLen  = BitConverter.ToUInt16(zoneData, (int)(offset + 4));
            byte nameLen = zoneData[offset + 6];

            // Validate record length
            if(recLen < 12) // Minimum valid record length
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid record length {0} at offset {1}", recLen, offset);

                break;
            }

            if(offset + recLen > validBytes)
            {
                AaruLogging.Debug(MODULE_NAME, "Record extends beyond valid data at offset {0}", offset);

                break;
            }

            // Validate name length
            if(nameLen < 1 || nameLen > XIAFS_NAME_LEN || nameLen + 8 > recLen)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Invalid name length {0} at offset {1} (rec_len={2})",
                                  nameLen,
                                  offset,
                                  recLen);

                offset += recLen;

                continue;
            }

            if(inoNum != 0)
            {
                // Extract filename
                var nameBytes = new byte[nameLen];
                Array.Copy(zoneData, offset + 7, nameBytes, 0, nameLen);
                string filename = StringHandlers.CToString(nameBytes, _encoding);

                // Skip "." and ".." entries
                if(!string.IsNullOrWhiteSpace(filename) && filename != "." && filename != "..")
                {
                    entries[filename] = inoNum;

                    AaruLogging.Debug(MODULE_NAME, "Directory entry: '{0}' -> inode {1}", filename, inoNum);
                }
            }

            offset += recLen;
        }
    }

    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class XiaDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }
}