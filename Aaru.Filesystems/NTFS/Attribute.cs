// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Attribute.cs
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
    /// <summary>
    ///     Finds all instances of a given attribute type (optionally with a specific name) across the base MFT record and
    ///     any extension records referenced by <c>$ATTRIBUTE_LIST</c>.
    /// </summary>
    /// <param name="mftRecordNumber">MFT record number of the base record.</param>
    /// <param name="targetType">Attribute type to search for.</param>
    /// <param name="targetName">
    ///     Optional attribute name to match. Pass <c>null</c> to match unnamed attributes only, or a
    ///     specific string to match named attributes.
    /// </param>
    /// <param name="results">List of found attributes, ordered by <c>lowest_vcn</c> for multi-extent attributes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindAttributes(uint                     mftRecordNumber, AttributeType targetType, string targetName,
                               out List<FoundAttribute> results)
    {
        results = [];

        ErrorNumber errno = ReadMftRecord(mftRecordNumber, out byte[] recordData);

        if(errno != ErrorNumber.NoError) return errno;

        MftRecord header =
            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(recordData, 0, Marshal.SizeOf<MftRecord>());

        if(header.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME, "MFT record {0} has invalid magic", mftRecordNumber);

            return ErrorNumber.InvalidArgument;
        }

        return FindAttributes(recordData, header, mftRecordNumber, targetType, targetName, out results);
    }

    /// <summary>
    ///     Finds all instances of a given attribute type (optionally with a specific name) across the base MFT record and
    ///     any extension records referenced by <c>$ATTRIBUTE_LIST</c>.
    /// </summary>
    /// <param name="recordData">Raw base MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="mftRecordNumber">MFT record number of the base record (for logging).</param>
    /// <param name="targetType">Attribute type to search for.</param>
    /// <param name="targetName">
    ///     Optional attribute name to match. Pass <c>null</c> to match unnamed attributes only, or a
    ///     specific string to match named attributes.
    /// </param>
    /// <param name="results">List of found attributes, ordered by <c>lowest_vcn</c> for multi-extent attributes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindAttributes(byte[] recordData, in  MftRecord header, uint mftRecordNumber, AttributeType targetType,
                               string targetName, out List<FoundAttribute> results)
    {
        results = [];

        // First, check if this record has an $ATTRIBUTE_LIST
        byte[] attrListData = ReadAttributeListData(recordData, header);

        if(attrListData == null)
        {
            // No $ATTRIBUTE_LIST — scan the base record only (simple case)
            ScanRecordForAttribute(recordData, header.attrs_offset, targetType, targetName, results);

            return ErrorNumber.NoError;
        }

        // Has $ATTRIBUTE_LIST — parse it to find all instances of the target attribute across extension records
        HashSet<uint> processedRecords = [mftRecordNumber];

        var alOffset    = 0;
        int alEntrySize = Marshal.SizeOf<AttributeListEntry>();

        while(alOffset + alEntrySize <= attrListData.Length)
        {
            AttributeListEntry entry =
                Marshal.ByteArrayToStructureLittleEndian<AttributeListEntry>(attrListData, alOffset, alEntrySize);

            // Validate entry
            if(entry.length < alEntrySize || alOffset + entry.length > attrListData.Length) break;

            if(entry.type == AttributeType.End || entry.type == AttributeType.Unused) break;

            // Check if this entry matches our target type
            if(entry.type == targetType)
            {
                // Check name match
                bool nameMatches;

                if(targetName == null)
                {
                    // Looking for unnamed attribute
                    nameMatches = entry.name_length == 0;
                }
                else
                {
                    // Looking for named attribute
                    if(entry.name_length > 0 && entry.name_offset + entry.name_length * 2 <= entry.length)
                    {
                        string entryName =
                            Encoding.Unicode.GetString(attrListData,
                                                       alOffset + entry.name_offset,
                                                       entry.name_length * 2);

                        nameMatches = string.Equals(entryName, targetName, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                        nameMatches = false;
                }

                if(nameMatches)
                {
                    // Extract MFT record number from the reference (lower 48 bits)
                    var refRecordNumber = (uint)(entry.mft_reference & 0x0000FFFFFFFFFFFF);

                    byte[] extRecordData;

                    if(refRecordNumber == mftRecordNumber)
                    {
                        // Attribute is in the base record
                        extRecordData = recordData;
                    }
                    else
                    {
                        // Need to load the extension record
                        if(!processedRecords.Add(refRecordNumber))
                        {
                            // Already processed this record for this attribute list entry
                            alOffset += entry.length;

                            continue;
                        }

                        ErrorNumber extErrno = ReadMftRecord(refRecordNumber, out extRecordData);

                        if(extErrno != ErrorNumber.NoError)
                        {
                            AaruLogging.Debug(MODULE_NAME,
                                              "Error reading extension MFT record {0}: {1}",
                                              refRecordNumber,
                                              extErrno);

                            alOffset += entry.length;

                            continue;
                        }

                        // Validate extension record
                        MftRecord extHeader =
                            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(extRecordData,
                                                                                    0,
                                                                                    Marshal.SizeOf<MftRecord>());

                        if(extHeader.magic != NtfsRecordMagic.File)
                        {
                            AaruLogging.Debug(MODULE_NAME,
                                              "Extension MFT record {0} has invalid magic",
                                              refRecordNumber);

                            alOffset += entry.length;

                            continue;
                        }
                    }

                    // Find the specific attribute in this record matching the instance number
                    FindAttributeByInstance(extRecordData,
                                            targetType,
                                            targetName,
                                            entry.instance,
                                            entry.lowest_vcn,
                                            results);
                }
            }

            alOffset += entry.length;
        }

        // Sort results by lowest_vcn for proper multi-extent assembly
        results.Sort((a, b) => a.LowestVcn.CompareTo(b.LowestVcn));

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Finds all instances of any attribute type across the base MFT record and any extension records referenced by
    ///     <c>$ATTRIBUTE_LIST</c>. Used by methods that need to enumerate all attributes (e.g., <c>Stat</c>,
    ///     <c>ListXAttr</c>).
    /// </summary>
    /// <param name="mftRecordNumber">MFT record number of the base record.</param>
    /// <param name="results">List of found attributes across all records.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindAllAttributes(uint mftRecordNumber, out List<FoundAttribute> results)
    {
        results = [];

        ErrorNumber errno = ReadMftRecord(mftRecordNumber, out byte[] recordData);

        if(errno != ErrorNumber.NoError) return errno;

        MftRecord header =
            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(recordData, 0, Marshal.SizeOf<MftRecord>());

        if(header.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME, "MFT record {0} has invalid magic", mftRecordNumber);

            return ErrorNumber.InvalidArgument;
        }

        return FindAllAttributes(recordData, header, mftRecordNumber, out results);
    }

    /// <summary>
    ///     Finds all instances of any attribute type across the base MFT record and any extension records referenced by
    ///     <c>$ATTRIBUTE_LIST</c>.
    /// </summary>
    /// <param name="recordData">Raw base MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="mftRecordNumber">MFT record number of the base record (for logging).</param>
    /// <param name="results">List of found attributes across all records.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindAllAttributes(byte[]                   recordData, in MftRecord header, uint mftRecordNumber,
                                  out List<FoundAttribute> results)
    {
        results = [];

        // First, check if this record has an $ATTRIBUTE_LIST
        byte[] attrListData = ReadAttributeListData(recordData, header);

        if(attrListData == null)
        {
            // No $ATTRIBUTE_LIST — scan the base record only
            ScanAllAttributesInRecord(recordData, header.attrs_offset, results);

            return ErrorNumber.NoError;
        }

        // Has $ATTRIBUTE_LIST — parse it to find all attributes across all extension records
        HashSet<uint>   processedRecords   = [mftRecordNumber];
        HashSet<ushort> processedInstances = [];

        var alOffset    = 0;
        int alEntrySize = Marshal.SizeOf<AttributeListEntry>();

        while(alOffset + alEntrySize <= attrListData.Length)
        {
            AttributeListEntry entry =
                Marshal.ByteArrayToStructureLittleEndian<AttributeListEntry>(attrListData, alOffset, alEntrySize);

            if(entry.length < alEntrySize || alOffset + entry.length > attrListData.Length) break;

            if(entry.type == AttributeType.End || entry.type == AttributeType.Unused) break;

            // Skip $ATTRIBUTE_LIST entries for $ATTRIBUTE_LIST itself
            if(entry.type == AttributeType.AttributeList)
            {
                alOffset += entry.length;

                continue;
            }

            var refRecordNumber = (uint)(entry.mft_reference & 0x0000FFFFFFFFFFFF);

            byte[] extRecordData;

            if(refRecordNumber == mftRecordNumber)
                extRecordData = recordData;
            else
            {
                ErrorNumber extErrno = ReadMftRecord(refRecordNumber, out extRecordData);

                if(extErrno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Error reading extension MFT record {0}: {1}",
                                      refRecordNumber,
                                      extErrno);

                    alOffset += entry.length;

                    continue;
                }

                MftRecord extHeader =
                    Marshal.ByteArrayToStructureLittleEndian<MftRecord>(extRecordData, 0, Marshal.SizeOf<MftRecord>());

                if(extHeader.magic != NtfsRecordMagic.File)
                {
                    AaruLogging.Debug(MODULE_NAME, "Extension MFT record {0} has invalid magic", refRecordNumber);

                    alOffset += entry.length;

                    continue;
                }
            }

            // Get attribute name from entry if present
            string entryName = null;

            if(entry.name_length > 0 && entry.name_offset + entry.name_length * 2 <= entry.length)
            {
                entryName = Encoding.Unicode.GetString(attrListData,
                                                       alOffset + entry.name_offset,
                                                       entry.name_length * 2);
            }

            // Avoid processing the same instance twice (can happen when multiple AL entries point to same record)
            if(!processedInstances.Add(entry.instance))
            {
                alOffset += entry.length;

                continue;
            }

            FindAttributeByInstance(extRecordData, entry.type, entryName, entry.instance, entry.lowest_vcn, results);

            alOffset += entry.length;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Assembles data runs from all extents of a non-resident attribute across base and extension MFT records, ordered
    ///     by <c>lowest_vcn</c>.
    /// </summary>
    /// <param name="mftRecordNumber">MFT record number of the base record.</param>
    /// <param name="targetType">Attribute type to search for.</param>
    /// <param name="targetName">
    ///     Optional attribute name to match. Pass <c>null</c> for unnamed attributes, or a specific
    ///     string for named attributes.
    /// </param>
    /// <param name="dataRuns">Assembled data runs from all extents.</param>
    /// <param name="dataSize">Logical data size from the first extent header.</param>
    /// <param name="allocatedSize">Allocated size from the first extent header.</param>
    /// <param name="flags">Attribute flags from the first extent header.</param>
    /// <param name="compressionUnit">Compression unit from the first extent header.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber AssembleNonResidentRuns(uint mftRecordNumber, AttributeType targetType, string targetName,
                                        out List<(long offset, long length)> dataRuns, out long dataSize,
                                        out long allocatedSize, out AttributeFlags flags, out byte compressionUnit)
    {
        dataRuns        = [];
        dataSize        = 0;
        allocatedSize   = 0;
        flags           = 0;
        compressionUnit = 0;

        ErrorNumber errno = FindAttributes(mftRecordNumber, targetType, targetName, out List<FoundAttribute> attrs);

        if(errno != ErrorNumber.NoError) return errno;

        if(attrs.Count == 0) return ErrorNumber.NoSuchFile;

        var gotHeader = false;

        foreach(FoundAttribute attr in attrs)
        {
            byte nonResident = attr.RecordData[attr.Offset + 8];

            if(nonResident == 0)
            {
                // If this is a resident attribute, it shouldn't be assembled via data runs
                // Return the first matching resident instance as a signal to the caller
                if(!gotHeader) return ErrorNumber.NoError;

                continue;
            }

            NonResidentAttributeRecord nrAttr =
                Marshal.ByteArrayToStructureLittleEndian<NonResidentAttributeRecord>(attr.RecordData,
                    attr.Offset,
                    Marshal.SizeOf<NonResidentAttributeRecord>());

            // Get size info from the first extent (lowest_vcn == 0)
            if(!gotHeader)
            {
                dataSize        = (long)nrAttr.data_size;
                allocatedSize   = (long)nrAttr.allocated_size;
                flags           = nrAttr.flags;
                compressionUnit = nrAttr.compression_unit;
                gotHeader       = true;
            }

            int runListOffset = attr.Offset + nrAttr.mapping_pairs_offset;
            int runListEnd    = attr.Offset + (int)nrAttr.length;

            List<(long offset, long length)> extentRuns = ParseDataRuns(attr.RecordData, runListOffset, runListEnd);
            dataRuns.AddRange(extentRuns);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the <c>$ATTRIBUTE_LIST</c> attribute data from an MFT record, or returns null if not present.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <returns>Raw attribute list data, or <c>null</c> if no <c>$ATTRIBUTE_LIST</c> exists.</returns>
    byte[] ReadAttributeListData(byte[] recordData, in MftRecord header)
    {
        int offset = header.attrs_offset;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            if(attrType == AttributeType.AttributeList)
            {
                byte nonResident = recordData[offset + 8];

                if(nonResident == 0)
                {
                    // Resident $ATTRIBUTE_LIST
                    var valueOffset = BitConverter.ToUInt16(recordData, offset + 0x14);
                    var valueLength = BitConverter.ToUInt32(recordData, offset + 0x10);

                    int valueStart = offset + valueOffset;

                    if(valueStart + valueLength > recordData.Length) return null;

                    var data = new byte[valueLength];
                    Array.Copy(recordData, valueStart, data, 0, valueLength);

                    return data;
                }

                // Non-resident $ATTRIBUTE_LIST
                NonResidentAttributeRecord nrAttr =
                    Marshal.ByteArrayToStructureLittleEndian<NonResidentAttributeRecord>(recordData,
                        offset,
                        Marshal.SizeOf<NonResidentAttributeRecord>());

                int runListOffset = offset + nrAttr.mapping_pairs_offset;

                List<(long offset, long length)> dataRuns =
                    ParseDataRuns(recordData, runListOffset, offset + (int)attrLength);

                byte[] buf = Array.Empty<byte>();

                ErrorNumber errno = ReadNonResidentData(dataRuns, (long)nrAttr.data_size, ref buf);

                return errno != ErrorNumber.NoError ? null : buf;
            }

            offset += (int)attrLength;
        }

        return null;
    }

    /// <summary>Scans a single MFT record for attributes matching the given type and name.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="attrsOffset">Offset to the first attribute in the record.</param>
    /// <param name="targetType">Attribute type to search for.</param>
    /// <param name="targetName">
    ///     Attribute name to match (<c>null</c> for unnamed attributes, or a specific string for named
    ///     attributes).
    /// </param>
    /// <param name="results">List to append matching attributes to.</param>
    static void ScanRecordForAttribute(byte[] recordData, int attrsOffset, AttributeType targetType, string targetName,
                                       List<FoundAttribute> results)
    {
        int offset = attrsOffset;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            if(attrType == targetType)
            {
                byte nameLength = recordData[offset + 9];

                if(targetName == null)
                {
                    // Looking for unnamed attribute
                    if(nameLength == 0)
                    {
                        byte  nonResident = recordData[offset + 8];
                        ulong lowestVcn   = 0;

                        if(nonResident != 0) lowestVcn = BitConverter.ToUInt64(recordData, offset + 0x10);

                        results.Add(new FoundAttribute(recordData, offset, lowestVcn));
                    }
                }
                else
                {
                    // Looking for named attribute
                    if(nameLength > 0)
                    {
                        var nameOffset = BitConverter.ToUInt16(recordData, offset + 0x0A);

                        if(offset + nameOffset + nameLength * 2 <= recordData.Length)
                        {
                            string name = Encoding.Unicode.GetString(recordData, offset + nameOffset, nameLength * 2);

                            if(string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
                            {
                                byte  nonResident = recordData[offset + 8];
                                ulong lowestVcn   = 0;

                                if(nonResident != 0) lowestVcn = BitConverter.ToUInt64(recordData, offset + 0x10);

                                results.Add(new FoundAttribute(recordData, offset, lowestVcn));
                            }
                        }
                    }
                }
            }

            offset += (int)attrLength;
        }
    }

    /// <summary>Scans a single MFT record and adds all attributes to the results list.</summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="attrsOffset">Offset to the first attribute in the record.</param>
    /// <param name="results">List to append all found attributes to.</param>
    static void ScanAllAttributesInRecord(byte[] recordData, int attrsOffset, List<FoundAttribute> results)
    {
        int offset = attrsOffset;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            byte  nonResident = recordData[offset + 8];
            ulong lowestVcn   = 0;

            if(nonResident != 0) lowestVcn = BitConverter.ToUInt64(recordData, offset + 0x10);

            results.Add(new FoundAttribute(recordData, offset, lowestVcn));

            offset += (int)attrLength;
        }
    }

    /// <summary>
    ///     Finds a specific attribute in an MFT record by its instance number. Used when traversing
    ///     <c>$ATTRIBUTE_LIST</c> to correlate entries with actual attribute records in extension records.
    /// </summary>
    /// <param name="recordData">Raw MFT record data.</param>
    /// <param name="targetType">Attribute type to match.</param>
    /// <param name="targetName">Attribute name to match (<c>null</c> for unnamed).</param>
    /// <param name="instance">Instance number from the attribute list entry.</param>
    /// <param name="lowestVcn">Lowest VCN from the attribute list entry.</param>
    /// <param name="results">List to append the matching attribute to.</param>
    static void FindAttributeByInstance(byte[] recordData, AttributeType targetType, string targetName, ushort instance,
                                        ulong  lowestVcn,  List<FoundAttribute> results)
    {
        // We need to find the attrs_offset from the MFT record header
        MftRecord header =
            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(recordData, 0, Marshal.SizeOf<MftRecord>());

        int offset = header.attrs_offset;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            if(attrType == targetType)
            {
                // Match by instance number
                var attrInstance = BitConverter.ToUInt16(recordData, offset + 0x0E);

                if(attrInstance == instance)
                {
                    // Verify name match as well for safety
                    byte nameLength = recordData[offset + 9];
                    bool nameOk;

                    if(targetName == null)
                        nameOk = nameLength == 0;
                    else if(nameLength > 0)
                    {
                        var nameOffset = BitConverter.ToUInt16(recordData, offset + 0x0A);

                        if(offset + nameOffset + nameLength * 2 <= recordData.Length)
                        {
                            string name = Encoding.Unicode.GetString(recordData, offset + nameOffset, nameLength * 2);

                            nameOk = string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                            nameOk = false;
                    }
                    else
                        nameOk = false;

                    if(nameOk)
                    {
                        results.Add(new FoundAttribute(recordData, offset, lowestVcn));

                        return;
                    }
                }
            }

            offset += (int)attrLength;
        }
    }

    /// <summary>Represents a found attribute within an MFT record.</summary>
    readonly struct FoundAttribute
    {
        /// <summary>The raw MFT record data containing this attribute.</summary>
        public readonly byte[] RecordData;

        /// <summary>Offset of this attribute's header within <see cref="RecordData" />.</summary>
        public readonly int Offset;

        /// <summary>Lowest VCN for this attribute extent (0 for single-extent or resident attributes).</summary>
        public readonly ulong LowestVcn;

        /// <summary>Initializes a new instance of the <see cref="FoundAttribute" /> struct.</summary>
        public FoundAttribute(byte[] recordData, int offset, ulong lowestVcn)
        {
            RecordData = recordData;
            Offset     = offset;
            LowestVcn  = lowestVcn;
        }
    }
}