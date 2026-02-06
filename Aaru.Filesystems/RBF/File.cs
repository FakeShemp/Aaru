// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class RBF
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.File;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Use Stat to get the file information
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/")
        {
            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                Inode            = _rootDirLsn,
                Length           = _rootDirectoryFd.fd_fsize,
                Links            = _rootDirectoryFd.fd_link,
                UID              = _rootDirectoryFd.fd_own,
                Mode             = _rootDirectoryFd.fd_att,
                CreationTimeUtc  = DateHandlers.Os9ToDateTime(_rootDirectoryFd.fd_dcr),
                LastWriteTimeUtc = DateHandlers.Os9ToDateTime(_rootDirectoryFd.fd_date)
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start traversal from root directory cache
        Dictionary<string, CachedDirectoryEntry> currentDirectory = _rootDirectoryCache;
        CachedDirectoryEntry                     targetEntry      = null;

        // Traverse all path components
        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            // Skip . and ..
            if(component == "." || component == "..") continue;

            // Find the component in current directory
            if(!currentDirectory.TryGetValue(component, out CachedDirectoryEntry entry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, we found our target
            if(i == pathComponents.Length - 1)
            {
                targetEntry = entry;

                break;
            }

            // Not the last component - must be a directory to continue traversal
            if(!entry.IsDirectory) return ErrorNumber.NotDirectory;

            // Read the subdirectory contents
            ErrorNumber errno = ReadDirectoryContents(entry.Fd, out Dictionary<string, CachedDirectoryEntry> subDir);

            if(errno != ErrorNumber.NoError) return errno;

            currentDirectory = subDir;
        }

        if(targetEntry == null) return ErrorNumber.NoSuchFile;

        // Build file attributes from OS-9 attributes
        // Bit 7: Directory, Bit 6: Single user, Bit 5: Public execute, Bit 4: Public write
        // Bit 3: Public read, Bit 2: Execute, Bit 1: Write, Bit 0: Read
        FileAttributes attributes = FileAttributes.None;

        if((targetEntry.Fd.fd_att & 0x80) != 0) attributes |= FileAttributes.Directory;

        // Map OS-9 permissions to generic attributes
        // If no write permission, mark as read-only
        if((targetEntry.Fd.fd_att & 0x02) == 0 && (targetEntry.Fd.fd_att & 0x10) == 0)
            attributes |= FileAttributes.ReadOnly;

        // If it's not a directory and has no special attributes, it's a regular file
        if(attributes == FileAttributes.None) attributes = FileAttributes.File;

        stat = new FileEntryInfo
        {
            Attributes       = attributes,
            Inode            = targetEntry.FdLsn,
            Length           = targetEntry.Fd.fd_fsize,
            Links            = targetEntry.Fd.fd_link,
            UID              = targetEntry.Fd.fd_own,
            Mode             = targetEntry.Fd.fd_att,
            CreationTimeUtc  = DateHandlers.Os9ToDateTime(targetEntry.Fd.fd_dcr),
            LastWriteTimeUtc = DateHandlers.Os9ToDateTime(targetEntry.Fd.fd_date)
        };

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the contents of a directory given its file descriptor</summary>
    /// <param name="fd">The file descriptor of the directory</param>
    /// <param name="entries">Dictionary of directory entries indexed by filename</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDirectoryContents(FileDescriptor fd, out Dictionary<string, CachedDirectoryEntry> entries)
    {
        entries = new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);

        // Parse the segment list from directory FD
        List<(uint lsn, uint sectors)> segments = ParseSegmentList(fd.fd_seg);

        if(segments.Count == 0) return ErrorNumber.NoError; // Empty directory is valid

        uint fileSize  = fd.fd_fsize;
        uint bytesRead = 0;

        foreach((uint lsn, uint sectors) in segments)
        {
            for(uint s = 0; s < sectors && bytesRead < fileSize; s++)
            {
                ErrorNumber errno = ReadLsn(lsn + s, out byte[] sectorData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Failed to read directory sector at LSN {0}: {1}", lsn + s, errno);

                    continue;
                }

                // Parse directory entries in this sector
                var entriesPerSector = (int)(_lsnSize / 32); // Each entry is 32 bytes

                for(var e = 0; e < entriesPerSector && bytesRead < fileSize; e++)
                {
                    int entryOffset = e * 32;

                    if(entryOffset + 32 > sectorData.Length) break;

                    // Check if entry is used (first byte != 0)
                    if(sectorData[entryOffset] == 0)
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Parse directory entry
                    DirectoryEntry dirEntry = _littleEndian
                                                  ? Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(sectorData,
                                                      entryOffset,
                                                      32)
                                                  : Marshal.ByteArrayToStructureBigEndian<DirectoryEntry>(sectorData,
                                                      entryOffset,
                                                      32);

                    // Extract filename (MSB of last char set indicates end)
                    string filename = ReadRbfFilename(dirEntry.dir_name);

                    if(string.IsNullOrEmpty(filename))
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Get FD LSN
                    uint fdLsn = LSNToUInt32(dirEntry.dir_fd);

                    if(fdLsn == 0)
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Read the file descriptor for this entry
                    errno = ReadLsn(fdLsn, out byte[] fdData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, "Failed to read FD for '{0}' at LSN {1}", filename, fdLsn);
                        bytesRead += 32;

                        continue;
                    }

                    FileDescriptor entryFd = _littleEndian
                                                 ? Marshal.ByteArrayToStructureLittleEndian<FileDescriptor>(fdData)
                                                 : Marshal.ByteArrayToStructureBigEndian<FileDescriptor>(fdData);

                    var cachedEntry = new CachedDirectoryEntry
                    {
                        Name     = filename,
                        FdLsn    = fdLsn,
                        Fd       = entryFd,
                        FileSize = entryFd.fd_fsize
                    };

                    // Use filename as key, handle duplicates
                    if(!entries.ContainsKey(filename)) entries[filename] = cachedEntry;

                    bytesRead += 32;
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an RBF filename from directory entry (MSB of last char set)</summary>
    static string ReadRbfFilename(byte[] nameBytes)
    {
        if(nameBytes == null || nameBytes.Length == 0) return null;

        var chars = new List<char>();

        foreach(byte b in nameBytes)
        {
            if(b == 0) break;

            // Check if MSB is set (indicates last character)
            if((b & 0x80) != 0)
            {
                chars.Add((char)(b & 0x7F));

                break;
            }

            chars.Add((char)b);
        }

        return chars.Count > 0 ? new string(chars.ToArray()) : null;
    }
}