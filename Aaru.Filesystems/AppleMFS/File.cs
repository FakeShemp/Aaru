// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Macintosh File System plugin.
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
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

// Information from Inside Macintosh Volume II
public sealed partial class AppleMFS
{
    ErrorNumber ReadFile(string path, out byte[] buf, bool resourceFork, bool tags)
    {
        buf = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        path = pathElements[0];

        if(!_filenameToId.TryGetValue(path.ToLowerInvariant(), out uint fileId)) return ErrorNumber.NoSuchFile;

        if(!_idToEntry.TryGetValue(fileId, out FileEntry entry)) return ErrorNumber.NoSuchFile;

        uint nextBlock;

        if(resourceFork)
        {
            if(entry.flRPyLen == 0)
            {
                buf = [];

                return ErrorNumber.NoError;
            }

            nextBlock = entry.flRStBlk;
        }
        else
        {
            if(entry.flPyLen == 0)
            {
                buf = [];

                return ErrorNumber.NoError;
            }

            nextBlock = entry.flStBlk;
        }

        var ms = new MemoryStream();

        do
        {
            ErrorNumber errno = tags
                                    ? _device.ReadSectorsTag((ulong)((nextBlock - 2) * _sectorsPerBlock) +
                                                             _volMdb.drAlBlSt                            +
                                                             _partitionStart,
                                                             false,
                                                             (uint)_sectorsPerBlock,
                                                             SectorTagType.AppleSonyTag,
                                                             out byte[] sectors)
                                    : _device.ReadSectors((ulong)((nextBlock - 2) * _sectorsPerBlock) +
                                                          _volMdb.drAlBlSt                            +
                                                          _partitionStart,
                                                          false,
                                                          (uint)_sectorsPerBlock,
                                                          out sectors,
                                                          out _);

            if(errno != ErrorNumber.NoError) return errno;

            ms.Write(sectors, 0, sectors.Length);

            if(_blockMap[nextBlock] == BMAP_FREE)
            {
                AaruLogging.Error(Localization.File_truncated_at_block_0, nextBlock);

                break;
            }

            nextBlock = _blockMap[nextBlock];
        } while(nextBlock > BMAP_LAST);

        if(tags)
            buf = ms.ToArray();
        else
        {
            if(resourceFork)
            {
                if(ms.Length < entry.flRLgLen)
                    buf = ms.ToArray();
                else
                {
                    buf = new byte[entry.flRLgLen];
                    Array.Copy(ms.ToArray(), 0, buf, 0, buf.Length);
                }
            }
            else
            {
                if(ms.Length < entry.flLgLen)
                    buf = ms.ToArray();
                else
                {
                    buf = new byte[entry.flLgLen];
                    Array.Copy(ms.ToArray(), 0, buf, 0, buf.Length);
                }
            }
        }

        return ErrorNumber.NoError;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        byte[]      file;
        ErrorNumber error = ErrorNumber.NoError;

        switch(_debug)
        {
            case true when string.Equals(path, "$", StringComparison.InvariantCulture):
                file = _directoryBlocks;

                break;
            case true when string.Equals(path, "$Boot", StringComparison.InvariantCulture) && _bootBlocks != null:
                file = _bootBlocks;

                break;
            case true when string.Equals(path, "$Bitmap", StringComparison.InvariantCulture):
                file = _blockMapBytes;

                break;
            case true when string.Compare(path, "$MDB", StringComparison.InvariantCulture) == 0:
                file = _mdbBlocks;

                break;
            default:
                error = ReadFile(path, out file, false, false);

                break;
        }

        if(error != ErrorNumber.NoError) return error;

        node = new AppleMfsFileNode
        {
            Path   = path,
            Length = file.Length,
            Offset = 0,
            _cache = file
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AppleMfsFileNode mynode) return ErrorNumber.InvalidArgument;

        mynode._cache = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        if(node is not AppleMfsFileNode mynode) return ErrorNumber.InvalidArgument;

        read = length;

        if(length + mynode.Offset >= mynode.Length) read = mynode.Length - mynode.Offset;

        Array.Copy(mynode._cache, mynode.Offset, buffer, 0, read);

        mynode.Offset += read;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        path = pathElements[0];

        if(_debug)
        {
            if(string.Equals(path, "$",       StringComparison.InvariantCulture) ||
               string.Equals(path, "$Boot",   StringComparison.InvariantCulture) ||
               string.Equals(path, "$Bitmap", StringComparison.InvariantCulture) ||
               string.Equals(path, "$MDB",    StringComparison.InvariantCulture))
            {
                stat = new FileEntryInfo
                {
                    BlockSize  = _device.Info.SectorSize,
                    Inode      = 0,
                    Links      = 1,
                    Attributes = FileAttributes.System
                };

                if(string.Equals(path, "$", StringComparison.InvariantCulture))
                {
                    stat.Blocks = _directoryBlocks.Length / stat.BlockSize + _directoryBlocks.Length % stat.BlockSize;

                    stat.Length = _directoryBlocks.Length;
                }
                else if(string.Equals(path, "$Bitmap", StringComparison.InvariantCulture))
                {
                    stat.Blocks = _blockMapBytes.Length / stat.BlockSize + _blockMapBytes.Length % stat.BlockSize;

                    stat.Length = _blockMapBytes.Length;
                }
                else if(string.Equals(path, "$Boot", StringComparison.InvariantCulture) && _bootBlocks != null)
                {
                    stat.Blocks = _bootBlocks.Length / stat.BlockSize + _bootBlocks.Length % stat.BlockSize;
                    stat.Length = _bootBlocks.Length;
                }
                else if(string.Equals(path, "$MDB", StringComparison.InvariantCulture))
                {
                    stat.Blocks = _mdbBlocks.Length / stat.BlockSize + _mdbBlocks.Length % stat.BlockSize;
                    stat.Length = _mdbBlocks.Length;
                }
                else
                    return ErrorNumber.InvalidArgument;

                return ErrorNumber.NoError;
            }
        }

        if(!_filenameToId.TryGetValue(path.ToLowerInvariant(), out uint fileId)) return ErrorNumber.NoSuchFile;

        if(!_idToEntry.TryGetValue(fileId, out FileEntry entry)) return ErrorNumber.NoSuchFile;

        string      path1 = path;
        ErrorNumber error;
        var         attr = new FileAttributes();

        if(!_mounted)
            error = ErrorNumber.AccessDenied;
        else
        {
            string[] pathElements1 = path1.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            if(pathElements1.Length != 1)
                error = ErrorNumber.NotSupported;
            else
            {
                path1 = pathElements1[0];

                if(!_filenameToId.TryGetValue(path1.ToLowerInvariant(), out uint fileId1))
                    error = ErrorNumber.NoSuchFile;
                else
                {
                    if(!_idToEntry.TryGetValue(fileId1, out FileEntry entry1))
                        error = ErrorNumber.NoSuchFile;
                    else
                    {
                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsAlias))
                            attr |= FileAttributes.Alias;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBundle))
                            attr |= FileAttributes.Bundle;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBeenInited))
                            attr |= FileAttributes.HasBeenInited;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasCustomIcon))
                            attr |= FileAttributes.HasCustomIcon;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasNoINITs))
                            attr |= FileAttributes.HasNoINITs;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsInvisible))
                            attr |= FileAttributes.Hidden;

                        if(entry1.flFlags.HasFlag(FileFlags.Locked)) attr |= FileAttributes.Immutable;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsOnDesk))
                            attr |= FileAttributes.IsOnDesk;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsShared))
                            attr |= FileAttributes.Shared;

                        if(entry1.flUsrWds.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsStationery))
                            attr |= FileAttributes.Stationery;

                        if(!attr.HasFlag(FileAttributes.Alias)  &&
                           !attr.HasFlag(FileAttributes.Bundle) &&
                           !attr.HasFlag(FileAttributes.Stationery))
                            attr |= FileAttributes.File;

                        attr |= FileAttributes.BlockUnits;

                        error = ErrorNumber.NoError;
                    }
                }
            }
        }

        if(error != ErrorNumber.NoError) return error;

        stat = new FileEntryInfo
        {
            Attributes    = attr,
            Blocks        = entry.flPyLen / _volMdb.drAlBlkSiz,
            BlockSize     = _volMdb.drAlBlkSiz,
            CreationTime  = DateHandlers.MacToDateTime(entry.flCrDat),
            Inode         = entry.flFlNum,
            LastWriteTime = DateHandlers.MacToDateTime(entry.flMdDat),
            Length        = entry.flLgLen,
            Links         = 1
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        return ErrorNumber.NotImplemented;
    }

#endregion
}