// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extent.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
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
using Aaru.Decoders;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class LisaFS
{
    /// <summary>Searches the disk for an extents file (or gets it from cache)</summary>
    /// <returns>Error.</returns>
    /// <param name="fileId">File identifier.</param>
    /// <param name="file">Extents file.</param>
    ErrorNumber ReadExtentsFile(short fileId, out ExtentFile file)
    {
        file = new ExtentFile();
        ErrorNumber errno;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(fileId < 4 || fileId == 4 && _mddf.fsversion != LISA_V2 && _mddf.fsversion != LISA_V1)
            return ErrorNumber.InvalidArgument;

        if(_extentCache.TryGetValue(fileId, out file)) return ErrorNumber.NoError;

        // A file ID that cannot be stored in the S-Records File
        if(fileId >= _srecords.Length) return ErrorNumber.InvalidArgument;

        ulong ptr = _srecords[fileId].extent_ptr;

        var searchExtentByTag = false;

        // Pointers are relative to MDDF
        if(ptr is not 0xFFFFFFFF and not 0x00000000)
            ptr += _mddf.mddf_block + _volumePrefix;
        else
        {
            if(_srecords[fileId].filesize == 0) return ErrorNumber.NoSuchFile;

            searchExtentByTag = true;
        }

        LisaTag.PriamTag extTag;
        byte[]           tag;

        // This happens on some disks.
        // This is a filesystem corruption that makes LisaOS crash on scavenge.
        // This code just allow to ignore that corruption by searching the Extents File using sector tags
        if(searchExtentByTag || ptr >= _device.Info.Sectors)
        {
            var found = false;

            for(ulong i = 0; i < _device.Info.Sectors; i++)
            {
                errno = ReadLisaSectorTag(i, out tag);

                if(errno != ErrorNumber.NoError) continue;

                DecodeTag(tag, out extTag);

                if(extTag.FileId != fileId * -1 || extTag.RelPage != 0) continue;

                ptr   = i;
                found = true;

                break;
            }

            if(!found) return ErrorNumber.InvalidArgument;
        }

        // Checks that the sector tag indicates its the Extents File we are searching for
        errno = ReadLisaSectorTag(ptr, out tag);

        if(errno != ErrorNumber.NoError) return errno;

        DecodeTag(tag, out extTag);

        if(extTag.FileId != (short)(-1 * fileId) || extTag.RelPage != 0)
        {
            var found = false;

            for(ulong i = 0; i < _device.Info.Sectors; i++)
            {
                errno = ReadLisaSectorTag(i, out tag);

                if(errno != ErrorNumber.NoError) continue;

                DecodeTag(tag, out extTag);

                if(extTag.FileId != fileId * -1 || extTag.RelPage != 0) continue;

                ptr   = i;
                found = true;

                break;
            }

            if(!found) return ErrorNumber.NoSuchFile;
        }

        errno = _mddf.fsversion == LISA_V1
                    ? ReadLisaSectors(ptr, 2, out byte[] sector)
                    : ReadLisaSector(ptr, out sector);

        if(errno != ErrorNumber.NoError) return errno;

        if(sector[0] >= 32 || sector[0] == 0) return ErrorNumber.InvalidArgument;

        file.filenameLen = sector[0];
        file.filename    = new byte[file.filenameLen];
        Array.Copy(sector, 0x01, file.filename, 0, file.filenameLen);
        file.version             = BigEndianBitConverter.ToUInt16(sector, 0x20);
        file.unique_id           = BigEndianBitConverter.ToUInt64(sector, 0x22);
        file.unknown2            = sector[0x2A];
        file.etype               = sector[0x2B];
        file.ftype               = (FileType)sector[0x2C];
        file.unknown3            = sector[0x2D];
        file.dtc                 = BigEndianBitConverter.ToUInt32(sector, 0x2E);
        file.dta                 = BigEndianBitConverter.ToUInt32(sector, 0x32);
        file.dtm                 = BigEndianBitConverter.ToUInt32(sector, 0x36);
        file.dtb                 = BigEndianBitConverter.ToUInt32(sector, 0x3A);
        file.dts                 = BigEndianBitConverter.ToUInt32(sector, 0x3E);
        file.serial              = BigEndianBitConverter.ToUInt32(sector, 0x42);
        file.killed              = sector[0x46];
        file.safety_on           = sector[0x47];
        file.protected_file      = sector[0x48];
        file.master              = sector[0x49];
        file.scavenged           = sector[0x4A];
        file.closed_by_os        = sector[0x4B];
        file.file_open           = sector[0x4C];
        file.result_scavenge_pad = sector[0x4D];
        file.result_scavenge     = BigEndianBitConverter.ToUInt16(sector, 0x4E);
        file.unusedi1            = BigEndianBitConverter.ToUInt16(sector, 0x50);
        file.system_type         = BigEndianBitConverter.ToUInt16(sector, 0x52);
        file.user_type           = BigEndianBitConverter.ToUInt16(sector, 0x54);
        file.user_subtype        = BigEndianBitConverter.ToUInt16(sector, 0x56);
        file.release_number      = BigEndianBitConverter.ToUInt16(sector, 0x58);
        file.build_number        = BigEndianBitConverter.ToUInt16(sector, 0x5A);
        file.compatibility_level = BigEndianBitConverter.ToUInt16(sector, 0x5C);
        file.revision_level      = BigEndianBitConverter.ToUInt16(sector, 0x5E);
        file.file_portion        = BigEndianBitConverter.ToUInt16(sector, 0x60);
        file.password_length     = sector[0x62];
        file.password            = new byte[8];
        Array.Copy(sector, 0x63, file.password, 0, 8);
        file.parent_id     = BigEndianBitConverter.ToUInt16(sector, 0x6B);
        file.parent_id_pad = sector[0x6D];
        file.fs_overhead   = BigEndianBitConverter.ToUInt16(sector, 0x6E);
        file.hint_padding  = new byte[16];
        Array.Copy(sector, 0x70, file.hint_padding, 0, 16);
        file.label_padding = BigEndianBitConverter.ToInt16(sector, 0x17E);
        file.LisaInfo      = new byte[128];
        Array.Copy(sector, 0x180, file.LisaInfo, 0, 128);

        var extentsCount = 0;
        int extentsOffset;

        if(_mddf.fsversion == LISA_V1)
        {
            file.length      = BigEndianBitConverter.ToInt32(sector, 0x200);
            file.phys_length = BigEndianBitConverter.ToInt32(sector, 0x204);
            extentsOffset    = 0x208;
        }
        else
        {
            file.length      = BigEndianBitConverter.ToInt32(sector, 0x80);
            file.phys_length = BigEndianBitConverter.ToInt32(sector, 0x84);
            extentsOffset    = 0x88;
        }

        for(var j = 0; j < 41; j++)
        {
            if(BigEndianBitConverter.ToInt16(sector, extentsOffset + j * 6 + 4) == 0) break;

            extentsCount++;
        }

        file.extents = new Extent[extentsCount];

        for(var j = 0; j < extentsCount; j++)
        {
            file.extents[j] = new Extent
            {
                start  = BigEndianBitConverter.ToInt32(sector, extentsOffset + j * 6),
                length = BigEndianBitConverter.ToInt16(sector, extentsOffset + j * 6 + 4)
            };
        }

        _extentCache.Add(fileId, file);

        if(!_debug) return ErrorNumber.NoError;

        if(_printedExtents.Contains(fileId)) return ErrorNumber.NoError;

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].filenameLen = {1}", fileId, file.filenameLen);

        AaruLogging.Debug(MODULE_NAME,
                          "ExtentFile[{0}].filename = {1}",
                          fileId,
                          StringHandlers.CToString(file.filename, _encoding));

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].version = 0x{1:X4}",    fileId, file.version);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].unique_id = 0x{1:X16}", fileId, file.unique_id);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].unknown2 = 0x{1:X2}",   fileId, file.unknown2);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].etype = 0x{1:X2}",      fileId, file.etype);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].ftype = {1}",           fileId, file.ftype);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].unknown3 = 0x{1:X2}",   fileId, file.unknown3);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].dtc = {1}",             fileId, file.dtc);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].dta = {1}",             fileId, file.dta);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].dtm = {1}",             fileId, file.dtm);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].dtb = {1}",             fileId, file.dtb);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].dts = {1}",             fileId, file.dts);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].serial = {1}",          fileId, file.serial);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].killed = 0x{1:X2}",     fileId, file.killed);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].safety_on = {1}",       fileId, file.safety_on      > 0);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].protected_file = {1}",  fileId, file.protected_file > 0);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].master = {1}",          fileId, file.master         > 0);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].scavenged = {1}",       fileId, file.scavenged      > 0);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].closed_by_os = {1}",    fileId, file.closed_by_os   > 0);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].file_open = {1}",       fileId, file.file_open      > 0);

        AaruLogging.Debug(MODULE_NAME,
                          "ExtentFile[{0}].result_scavenge_pad = 0x{1:X2}",
                          fileId,
                          file.result_scavenge_pad);

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].result_scavenge = {1}",     fileId, file.result_scavenge);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].unusedi1 = {1}",            fileId, file.unusedi1);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].system_type = {1}",         fileId, file.system_type);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].user_type = {1}",           fileId, file.user_type);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].user_subtype = {1}",        fileId, file.user_subtype);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].release_number = {1}",      fileId, file.release_number);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].build_number = {1}",        fileId, file.build_number);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].compatibility_level = {1}", fileId, file.compatibility_level);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].revision_level = {1}",      fileId, file.revision_level);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].file_portion = 0x{1:X4}",   fileId, file.file_portion);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].password_length = {1}",     fileId, file.password_length);

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].password = {1}", fileId, _encoding.GetString(file.password));

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].parent_id = {1}",          fileId, file.parent_id);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].parent_id_pad = 0x{1:X2}", fileId, file.parent_id_pad);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].fs_overhead = {1}",        fileId, file.fs_overhead);

        AaruLogging.Debug(MODULE_NAME,
                          "ExtentFile[{0}].hint_padding = 0x{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}{8:X2}{9:X2}" +
                          "{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}{16:X2}",
                          fileId,
                          file.hint_padding[0],
                          file.hint_padding[1],
                          file.hint_padding[2],
                          file.hint_padding[3],
                          file.hint_padding[4],
                          file.hint_padding[5],
                          file.hint_padding[6],
                          file.hint_padding[7],
                          file.hint_padding[8],
                          file.hint_padding[9],
                          file.hint_padding[10],
                          file.hint_padding[11],
                          file.hint_padding[12],
                          file.hint_padding[13],
                          file.hint_padding[14],
                          file.hint_padding[15]);

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].length = {1}",      fileId, file.length);
        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].phys_length = {1}", fileId, file.phys_length);

        for(var ext = 0; ext < file.extents.Length; ext++)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ExtentFile[{0}].extents[{1}].start = {2}",
                              fileId,
                              ext,
                              file.extents[ext].start);

            AaruLogging.Debug(MODULE_NAME,
                              "ExtentFile[{0}].extents[{1}].length = {2}",
                              fileId,
                              ext,
                              file.extents[ext].length);
        }

        AaruLogging.Debug(MODULE_NAME, "ExtentFile[{0}].label_padding = 0x{1:X4}", fileId, file.label_padding);

        _printedExtents.Add(fileId);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads all the S-Records and caches it</summary>
    ErrorNumber ReadSRecords()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        // Searches the S-Records place using MDDF pointers
        ErrorNumber errno = ReadLisaSectors(_mddf.srec_ptr + _mddf.mddf_block + _volumePrefix,
                                            _mddf.srec_len,
                                            out byte[] sectors);

        if(errno != ErrorNumber.NoError) return errno;

        // Each entry takes 14 bytes
        _srecords = new SRecord[sectors.Length / 14];

        for(var s = 0; s < _srecords.Length; s++)
        {
            _srecords[s] = new SRecord
            {
                extent_ptr = BigEndianBitConverter.ToUInt32(sectors, 0x00 + 14 * s),
                unknown    = BigEndianBitConverter.ToUInt32(sectors, 0x04 + 14 * s),
                filesize   = BigEndianBitConverter.ToUInt32(sectors, 0x08 + 14 * s),
                flags      = BigEndianBitConverter.ToUInt16(sectors, 0x0C + 14 * s)
            };
        }

        return ErrorNumber.NoError;
    }
}