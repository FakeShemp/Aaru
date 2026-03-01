// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattrs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    const string EA_PREFIX  = "com.ibm.os2.";
    const string NT_ACL     = "com.microsoft.ntacl";
    const string EA_LXUID   = "$LXUID";
    const string EA_LXGID   = "$LXGID";
    const string EA_LXMOD   = "$LXMOD";
    const string EA_LXDEV   = "$LXDEV";
    const string EA_LXATTRB = "$LXATTRB";

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        uint mftRecordNumber;

        if(normalizedPath == "/")
            mftRecordNumber = (uint)SystemFileNumber.Root;
        else
        {
            ErrorNumber resolveErrno = ResolvePathToMftRecord(normalizedPath, out mftRecordNumber);

            if(resolveErrno != ErrorNumber.NoError) return resolveErrno;
        }

        xattrs = [];

        // Find all attributes across base + extension records
        ErrorNumber findErrno = FindAllAttributes(mftRecordNumber, out List<FoundAttribute> attrs);

        if(findErrno != ErrorNumber.NoError) return findErrno;

        foreach(FoundAttribute attr in attrs)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(attr.RecordData, attr.Offset);

            byte nonResident = attr.RecordData[attr.Offset + 8];

            switch(attrType)
            {
                // Security descriptor → expose as "com.microsoft.ntacl"
                case AttributeType.SecurityDescriptor when nonResident == 0:
                {
                    var valueLength = BitConverter.ToUInt32(attr.RecordData, attr.Offset + 0x10);

                    if(valueLength > 0) xattrs.Add(NT_ACL);

                    break;
                }

                // Extended Attributes → expose each EA prefixed with "com.ibm.os2."
                case AttributeType.Ea:
                {
                    ErrorNumber eaErrno = ReadEaAttributeData(attr.RecordData,
                                                              attr.Offset,
                                                              nonResident,
                                                              out byte[] eaData,
                                                              out int eaLength);

                    if(eaErrno == ErrorNumber.NoError && eaLength > 0) EnumerateEas(eaData, 0, eaLength, xattrs);

                    break;
                }

                // Named $DATA attributes → Alternate Data Streams (no prefix)
                case AttributeType.Data:
                {
                    byte nameLength = attr.RecordData[attr.Offset + 9];

                    if(nameLength > 0)
                    {
                        var nameOffset = BitConverter.ToUInt16(attr.RecordData, attr.Offset + 0x0A);

                        string streamName =
                            Encoding.Unicode.GetString(attr.RecordData, attr.Offset + nameOffset, nameLength * 2);

                        xattrs.Add(streamName);
                    }

                    break;
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrWhiteSpace(xattr)) return ErrorNumber.InvalidArgument;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        uint mftRecordNumber;

        if(normalizedPath == "/")
            mftRecordNumber = (uint)SystemFileNumber.Root;
        else
        {
            ErrorNumber resolveErrno = ResolvePathToMftRecord(normalizedPath, out mftRecordNumber);

            if(resolveErrno != ErrorNumber.NoError) return resolveErrno;
        }

        ErrorNumber errno = ReadMftRecord(mftRecordNumber, out byte[] recordData);

        if(errno != ErrorNumber.NoError) return errno;

        MftRecord header =
            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(recordData, 0, Marshal.SizeOf<MftRecord>());

        if(header.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME, "MFT record {0} has invalid magic", mftRecordNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Determine what kind of xattr is being requested
        if(xattr == NT_ACL) return ReadSecurityDescriptor(recordData, header, mftRecordNumber, ref buf);

        if(xattr.StartsWith(EA_PREFIX, StringComparison.Ordinal))
        {
            string eaName = xattr[EA_PREFIX.Length..];

            // WSL metadata EAs are not exposed as xattrs
            if(eaName is EA_LXUID or EA_LXGID or EA_LXMOD or EA_LXDEV or EA_LXATTRB)
                return ErrorNumber.NoSuchExtendedAttribute;

            return ReadEa(recordData, header, mftRecordNumber, eaName, ref buf);
        }

        // Otherwise it's an Alternate Data Stream name
        return ReadAlternateDataStream(recordData, header, mftRecordNumber, xattr, ref buf);
    }

    /// <summary>Reads the raw security descriptor ($SECURITY_DESCRIPTOR attribute) from an MFT record.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="mftRecordNumber">MFT record number (for attribute list traversal).</param>
    /// <param name="buf">Output buffer for the raw security descriptor bytes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadSecurityDescriptor(byte[] recordData, in MftRecord header, uint mftRecordNumber, ref byte[] buf)
    {
        ErrorNumber findErrno = FindAttributes(recordData,
                                               header,
                                               mftRecordNumber,
                                               AttributeType.SecurityDescriptor,
                                               null,
                                               out List<FoundAttribute> results);

        if(findErrno != ErrorNumber.NoError || results.Count == 0) return ErrorNumber.NoSuchExtendedAttribute;

        FoundAttribute attr        = results[0];
        byte           nonResident = attr.RecordData[attr.Offset + 8];

        if(nonResident != 0) return ErrorNumber.NoSuchExtendedAttribute;

        var valueOffset = BitConverter.ToUInt16(attr.RecordData, attr.Offset + 0x14);
        var valueLength = BitConverter.ToUInt32(attr.RecordData, attr.Offset + 0x10);

        int valueStart = attr.Offset + valueOffset;

        if(valueStart + valueLength > attr.RecordData.Length) return ErrorNumber.NoSuchExtendedAttribute;

        buf = new byte[valueLength];
        Array.Copy(attr.RecordData, valueStart, buf, 0, valueLength);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a specific Extended Attribute value from the $EA attribute in an MFT record.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="mftRecordNumber">MFT record number (for attribute list traversal).</param>
    /// <param name="eaName">EA name to search for (without the "com.ibm.os2." prefix).</param>
    /// <param name="buf">Output buffer for the EA value bytes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadEa(byte[] recordData, in MftRecord header, uint mftRecordNumber, string eaName, ref byte[] buf)
    {
        ErrorNumber findErrno = FindAttributes(recordData,
                                               header,
                                               mftRecordNumber,
                                               AttributeType.Ea,
                                               null,
                                               out List<FoundAttribute> results);

        if(findErrno != ErrorNumber.NoError || results.Count == 0) return ErrorNumber.NoSuchExtendedAttribute;

        FoundAttribute attr        = results[0];
        byte           nonResident = attr.RecordData[attr.Offset + 8];

        ErrorNumber eaErrno =
            ReadEaAttributeData(attr.RecordData, attr.Offset, nonResident, out byte[] eaData, out int eaLength);

        if(eaErrno != ErrorNumber.NoError) return eaErrno;

        if(eaLength > 0) return FindEaByName(eaData, 0, eaLength, eaName, ref buf);

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Reads a named Alternate Data Stream from an MFT record.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="mftRecordNumber">MFT record number (for attribute list traversal).</param>
    /// <param name="streamName">ADS name to search for.</param>
    /// <param name="buf">Output buffer for the stream data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadAlternateDataStream(byte[] recordData, in MftRecord header, uint mftRecordNumber, string streamName,
                                        ref byte[] buf)
    {
        ErrorNumber findErrno = FindAttributes(recordData,
                                               header,
                                               mftRecordNumber,
                                               AttributeType.Data,
                                               streamName,
                                               out List<FoundAttribute> results);

        if(findErrno != ErrorNumber.NoError || results.Count == 0) return ErrorNumber.NoSuchExtendedAttribute;

        FoundAttribute attr        = results[0];
        byte           nonResident = attr.RecordData[attr.Offset + 8];

        if(nonResident == 0)
        {
            // Resident ADS
            var valueOffset = BitConverter.ToUInt16(attr.RecordData, attr.Offset + 0x14);
            var valueLength = BitConverter.ToUInt32(attr.RecordData, attr.Offset + 0x10);

            int valueStart = attr.Offset + valueOffset;

            if(valueStart + valueLength > attr.RecordData.Length) return ErrorNumber.NoSuchExtendedAttribute;

            buf = new byte[valueLength];
            Array.Copy(attr.RecordData, valueStart, buf, 0, valueLength);

            return ErrorNumber.NoError;
        }

        // Non-resident ADS — assemble data runs from all extents
        ErrorNumber asmErrno = AssembleNonResidentRuns(mftRecordNumber,
                                                       AttributeType.Data,
                                                       streamName,
                                                       out List<(long offset, long length)> dataRuns,
                                                       out long dataSize,
                                                       out _,
                                                       out _,
                                                       out _);

        if(asmErrno != ErrorNumber.NoError) return asmErrno;

        return ReadNonResidentData(dataRuns, dataSize, ref buf);
    }

    /// <summary>Enumerates EA entries and adds their names (prefixed with "com.ibm.os2.") to the xattr list.</summary>
    /// <param name="data">Buffer containing the EA data.</param>
    /// <param name="start">Start offset of the EA data in the buffer.</param>
    /// <param name="length">Total length of the EA data.</param>
    /// <param name="xattrs">List to add xattr names to.</param>
    static void EnumerateEas(byte[] data, int start, int length, List<string> xattrs)
    {
        int pos = start;
        int end = start + length;

        while(pos + Marshal.SizeOf<EaAttribute>() <= end)
        {
            EaAttribute ea =
                Marshal.ByteArrayToStructureLittleEndian<EaAttribute>(data, pos, Marshal.SizeOf<EaAttribute>());

            int nameStart = pos + Marshal.SizeOf<EaAttribute>();

            if(nameStart + ea.ea_name_length > end) break;

            string eaName = Encoding.ASCII.GetString(data, nameStart, ea.ea_name_length);

            // Skip WSL metadata EAs — they are parsed into Stat fields instead
            if(eaName is EA_LXUID or EA_LXGID or EA_LXMOD or EA_LXDEV or EA_LXATTRB)
            {
                if(ea.next_entry_offset == 0) break;

                pos += (int)ea.next_entry_offset;

                continue;
            }

            xattrs.Add(EA_PREFIX + eaName);

            if(ea.next_entry_offset == 0) break;

            pos += (int)ea.next_entry_offset;
        }
    }

    /// <summary>Finds an EA by name and returns its value.</summary>
    /// <param name="data">Buffer containing the EA data.</param>
    /// <param name="start">Start offset of the EA data in the buffer.</param>
    /// <param name="length">Total length of the EA data.</param>
    /// <param name="eaName">EA name to search for.</param>
    /// <param name="buf">Output buffer for the EA value bytes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    static ErrorNumber FindEaByName(byte[] data, int start, int length, string eaName, ref byte[] buf)
    {
        int pos = start;
        int end = start + length;

        while(pos + Marshal.SizeOf<EaAttribute>() <= end)
        {
            EaAttribute ea =
                Marshal.ByteArrayToStructureLittleEndian<EaAttribute>(data, pos, Marshal.SizeOf<EaAttribute>());

            int nameStart = pos + Marshal.SizeOf<EaAttribute>();

            if(nameStart + ea.ea_name_length > end) break;

            string name = Encoding.ASCII.GetString(data, nameStart, ea.ea_name_length);

            if(string.Equals(name, eaName, StringComparison.OrdinalIgnoreCase))
            {
                // EA value follows the name + NUL terminator
                int valueStart = nameStart + ea.ea_name_length + 1;

                if(valueStart + ea.ea_value_length > end) return ErrorNumber.InvalidArgument;

                buf = new byte[ea.ea_value_length];
                Array.Copy(data, valueStart, buf, 0, ea.ea_value_length);

                return ErrorNumber.NoError;
            }

            if(ea.next_entry_offset == 0) break;

            pos += (int)ea.next_entry_offset;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Reads EA attribute data from either resident or non-resident storage.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="attrOffset">Offset of the EA attribute header within the record.</param>
    /// <param name="nonResident">Non-resident flag (0 = resident).</param>
    /// <param name="eaData">Output buffer containing the raw EA data.</param>
    /// <param name="eaLength">Output length of valid EA data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadEaAttributeData(byte[]  recordData, int attrOffset, byte nonResident, out byte[] eaData,
                                    out int eaLength)
    {
        eaData   = null;
        eaLength = 0;

        if(nonResident == 0)
        {
            // Resident EA
            var valueOffset = BitConverter.ToUInt16(recordData, attrOffset + 0x14);
            var valueLength = BitConverter.ToUInt32(recordData, attrOffset + 0x10);

            int valueStart = attrOffset + valueOffset;

            if(valueStart + valueLength > recordData.Length) return ErrorNumber.InvalidArgument;

            eaData   = recordData;
            eaLength = (int)valueLength;

            // Adjust: EnumerateEas/FindEaByName/ParseWslEas expect start=0 for the EA data buffer
            // but here eaData is the full record, so we need to slice it
            var slice = new byte[valueLength];
            Array.Copy(recordData, valueStart, slice, 0, valueLength);
            eaData   = slice;
            eaLength = (int)valueLength;

            return ErrorNumber.NoError;
        }

        // Non-resident EA — read via data runs
        var attrLength = BitConverter.ToUInt32(recordData, attrOffset + 4);

        NonResidentAttributeRecord nrAttr =
            Marshal.ByteArrayToStructureLittleEndian<NonResidentAttributeRecord>(recordData,
                attrOffset,
                Marshal.SizeOf<NonResidentAttributeRecord>());

        int runListOffset = attrOffset + nrAttr.mapping_pairs_offset;

        List<(long offset, long length)> dataRuns =
            ParseDataRuns(recordData, runListOffset, attrOffset + (int)attrLength);

        byte[] buf = Array.Empty<byte>();

        ErrorNumber errno = ReadNonResidentData(dataRuns, (long)nrAttr.data_size, ref buf);

        if(errno != ErrorNumber.NoError) return errno;

        eaData   = buf;
        eaLength = (int)nrAttr.data_size;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads non-resident data from disk using parsed data runs.</summary>
    /// <param name="dataRuns">List of (absolute cluster offset, length in clusters) tuples.</param>
    /// <param name="dataSize">Logical data size in bytes.</param>
    /// <param name="buf">Output buffer for the data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadNonResidentData(List<(long offset, long length)> dataRuns, long dataSize, ref byte[] buf)
    {
        buf = new byte[dataSize];

        long bytesRead = 0;

        foreach((long clusterOffset, long clusterLength) in dataRuns)
        {
            long runBytes = clusterLength * _bytesPerCluster;

            // Sparse run
            if(clusterOffset == 0)
            {
                long toClear = Math.Min(runBytes, dataSize - bytesRead);
                Array.Clear(buf, (int)bytesRead, (int)toClear);
                bytesRead += toClear;

                continue;
            }

            for(long c = 0; c < clusterLength && bytesRead < dataSize; c++)
            {
                ulong sectorStart = (ulong)(clusterOffset + c) * _sectorsPerCluster;

                ErrorNumber errno = _image.ReadSectors(_partition.Start + sectorStart,
                                                       false,
                                                       _sectorsPerCluster,
                                                       out byte[] clusterData,
                                                       out _);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading cluster {0}: {1}", clusterOffset + c, errno);

                    return errno;
                }

                long toCopy = Math.Min(_bytesPerCluster, dataSize - bytesRead);
                Array.Copy(clusterData, 0, buf, bytesRead, toCopy);
                bytesRead += toCopy;
            }
        }

        return ErrorNumber.NoError;
    }
}