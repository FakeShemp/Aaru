using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Zip
{
    List<Entry> _entries;

    void FindEndOfCentralDirectory(out long eocdOffset, out long zip64LocOffset)
    {
        eocdOffset     = -1;
        zip64LocOffset = -1;

        long end = _stream.Length;

        // Search backward in chunks for the EOCD signature
        long chunkEnd = end;

        while(chunkEnd > 0)
        {
            int numBytes = EOCD_SEARCH_CHUNK;

            if(chunkEnd - numBytes < 0) numBytes = (int)chunkEnd;

            long chunkStart = chunkEnd - numBytes;

            // Overlap by 20 bytes to catch ZIP64 locator that might span chunk boundaries
            int preservedBytes = chunkStart >= 20 ? 20 : 0;

            var buf = new byte[numBytes];
            _stream.Position = chunkStart;

            int bytesRead = _stream.Read(buf, 0, numBytes);

            if(bytesRead < 4) break;

            // Search for PK\x05\x06 (EOCD signature)
            for(int pos = bytesRead - 4; pos >= preservedBytes; pos--)
            {
                if(buf[pos] != 0x50 || buf[pos + 1] != 0x4B || buf[pos + 2] != 0x05 || buf[pos + 3] != 0x06) continue;

                eocdOffset = chunkStart + pos;

                // Check for ZIP64 end of central directory locator 20 bytes before EOCD
                if(pos           >= 20   &&
                   buf[pos - 20] == 0x50 &&
                   buf[pos - 19] == 0x4B &&
                   buf[pos - 18] == 0x06 &&
                   buf[pos - 17] == 0x07)
                    zip64LocOffset = chunkStart + pos - 20;

                return;
            }

            chunkEnd -= numBytes - preservedBytes * 2;
        }
    }

    void ParseCentralDirectory(long eocdOffset, long zip64LocOffset)
    {
        var reader = new BinaryReader(_stream, Encoding.UTF8, true);

        // Read EOCD record
        _stream.Position = eocdOffset + 4; // skip signature

        ushort diskNumber       = reader.ReadUInt16();
        ushort centralDirDisk   = reader.ReadUInt16();
        ushort entriesOnDisk    = reader.ReadUInt16();
        long   totalEntries     = reader.ReadUInt16();
        long   centralDirSize   = reader.ReadUInt32();
        long   centralDirOffset = reader.ReadUInt32();
        ushort commentLength    = reader.ReadUInt16();

        if(commentLength > 0)
        {
            byte[] commentBytes = reader.ReadBytes(commentLength);
            _archiveComment = Encoding.UTF8.GetString(commentBytes);
        }

        // If we have a ZIP64 locator, read the ZIP64 EOCD record
        if(zip64LocOffset >= 0)
        {
            _stream.Position = zip64LocOffset + 4; // skip signature

            uint zip64EocdDisk   = reader.ReadUInt32();
            long zip64EocdOffset = reader.ReadInt64();

            _stream.Position = zip64EocdOffset;
            uint zip64Sig = reader.ReadUInt32();

            if(zip64Sig == ZIP64_EOCD_SIG)
            {
                reader.ReadInt64();  // size of ZIP64 EOCD record
                reader.ReadUInt16(); // version made by
                reader.ReadUInt16(); // version needed
                reader.ReadUInt32(); // disk number
                centralDirDisk = (ushort)reader.ReadUInt32();
                reader.ReadInt64(); // entries on disk
                totalEntries     = reader.ReadInt64();
                centralDirSize   = reader.ReadInt64();
                centralDirOffset = reader.ReadInt64();
            }
        }

        // Seek to the start of the central directory
        _stream.Position = centralDirOffset;

        for(long i = 0; i < totalEntries; i++)
        {
            if(_stream.Position >= _stream.Length) break;

            Entry entry = ReadCentralDirectoryRecord(reader);

            if(entry.Filename is null) break;

            long savedPos = _stream.Position;

            // Read local header to get exact data offset
            _stream.Position = entry.DataOffset; // locheaderoffset stored temporarily in DataOffset
            uint localSig = reader.ReadUInt32();

            if(localSig == LOCAL_HEADER_SIG || localSig == EOCD_SIG)
            {
                reader.ReadUInt16(); // version needed
                reader.ReadUInt16(); // flags
                reader.ReadUInt16(); // compression method
                uint localDate = reader.ReadUInt32();
                reader.ReadUInt32(); // crc32
                reader.ReadUInt32(); // compressed size
                reader.ReadUInt32(); // uncompressed size
                ushort localNameLen  = reader.ReadUInt16();
                ushort localExtraLen = reader.ReadUInt16();

                long dataOffset = _stream.Position + localNameLen + localExtraLen;

                // Parse local extra fields for additional metadata (timestamps, unicode paths, etc.)
                if(localNameLen > 0) reader.ReadBytes(localNameLen);

                if(localExtraLen > 0)
                {
                    long extraStart = _stream.Position;
                    ParseExtraFields(reader, localExtraLen, ref entry);
                    _stream.Position = extraStart + localExtraLen;
                }

                entry.DataOffset = dataOffset;
            }

            // Handle overflow for archives with >65535 entries without ZIP64
            if(i == totalEntries - 1 && zip64LocOffset < 0)
            {
                long remaining = centralDirOffset + centralDirSize - savedPos;

                if(remaining > 65536 * CENTRAL_DIR_ENTRY_SIZE) totalEntries += 65536;
            }

            _entries.Add(entry);

            _stream.Position = savedPos;
        }
    }

    Entry ReadCentralDirectoryRecord(BinaryReader reader)
    {
        Entry entry = new();

        uint sig = reader.ReadUInt32();

        if(sig != CENTRAL_DIR_SIG) return entry;

        byte   creatorVersion = reader.ReadByte();
        byte   system         = reader.ReadByte();
        ushort extractVersion = reader.ReadUInt16();
        ushort flags          = reader.ReadUInt16();
        ushort method         = reader.ReadUInt16();
        uint   dosDateTime    = reader.ReadUInt32();
        uint   crc32          = reader.ReadUInt32();
        long   compSize       = reader.ReadUInt32();
        long   uncompSize     = reader.ReadUInt32();
        ushort nameLength     = reader.ReadUInt16();
        ushort extraLength    = reader.ReadUInt16();
        ushort commentLength  = reader.ReadUInt16();
        ushort startDisk      = reader.ReadUInt16();
        ushort intFileAttrib  = reader.ReadUInt16();
        uint   extFileAttrib  = reader.ReadUInt32();
        long   locHeaderOff   = reader.ReadUInt32();

        // Read filename
        byte[] nameBytes = nameLength > 0 ? reader.ReadBytes(nameLength) : null;

        // Parse extra fields in central directory (primarily for ZIP64 overrides)
        int extraRemaining = extraLength;

        while(extraRemaining >= 4)
        {
            ushort extId   = reader.ReadUInt16();
            ushort extSize = reader.ReadUInt16();
            extraRemaining -= 4;

            if(extSize > extraRemaining) break;

            extraRemaining -= extSize;
            long nextExtra = _stream.Position + extSize;

            if(extId == EXTRA_ZIP64)
            {
                if(uncompSize == 0xFFFFFFFF && _stream.Position + 8 <= nextExtra) uncompSize = reader.ReadInt64();

                if(compSize == 0xFFFFFFFF && _stream.Position + 8 <= nextExtra) compSize = reader.ReadInt64();

                if(locHeaderOff == 0xFFFFFFFF && _stream.Position + 8 <= nextExtra) locHeaderOff = reader.ReadInt64();

                if(startDisk == 0xFFFF && _stream.Position + 4 <= nextExtra) startDisk = (ushort)reader.ReadUInt32();
            }

            _stream.Position = nextExtra;
        }

        if(extraRemaining > 0) _stream.Position += extraRemaining;

        // Read comment
        string comment = null;

        if(commentLength > 0)
        {
            byte[] commentBytes = reader.ReadBytes(commentLength);

            comment = (flags & FLAG_UTF8) != 0
                          ? Encoding.UTF8.GetString(commentBytes)
                          : _encoding.GetString(commentBytes);
        }

        // Decode filename
        string filename = null;

        if(nameBytes is not null)
        {
            filename = (flags & FLAG_UTF8) != 0 ? Encoding.UTF8.GetString(nameBytes) : _encoding.GetString(nameBytes);

            // Normalize path separators
            filename = filename.Replace('\\', '/');
        }

        // Detect directory
        var isDirectory = false;

        if(filename is not null && filename.EndsWith('/') && uncompSize == 0) isDirectory = true;

        if(system == (byte)HostOs.MsDos && (extFileAttrib & ATTR_DIRECTORY) != 0 && compSize == 0 && uncompSize == 0)
            isDirectory = true;

        // Extract Unix permissions from external attributes for Unix-created archives
        ushort unixPerms = 0;

        if(system == (byte)HostOs.Unix)
        {
            var perm = (int)(extFileAttrib >> 16);

            if(perm != 0) unixPerms = (ushort)perm;
        }

        entry.Method             = (CompressionMethod)method;
        entry.Filename           = filename;
        entry.CompressedSize     = compSize;
        entry.UncompressedSize   = uncompSize;
        entry.DataOffset         = locHeaderOff; // temporary, resolved to actual data offset later
        entry.LastWriteTime      = DosToDateTime(dosDateTime);
        entry.Crc32              = crc32;
        entry.System             = (HostOs)system;
        entry.ExternalAttributes = extFileAttrib;
        entry.UnixPermissions    = unixPerms;
        entry.Flags              = flags;
        entry.Comment            = comment;
        entry.IsDirectory        = isDirectory;
        entry.IsEncrypted        = (flags & FLAG_ENCRYPTED) != 0;

        return entry;
    }

    void ParseWithoutCentralDirectory()
    {
        var reader = new BinaryReader(_stream, Encoding.UTF8, true);

        _stream.Position = 0;

        while(_stream.Position < _stream.Length - 4)
        {
            uint sig;

            try
            {
                sig = reader.ReadUInt32();
            }
            catch(EndOfStreamException)
            {
                break;
            }

            switch(sig)
            {
                case LOCAL_HEADER_SIG:
                case EOCD_SIG: // kludge for strange archives
                {
                    ushort extractVersion = reader.ReadUInt16();
                    ushort flags          = reader.ReadUInt16();
                    ushort method         = reader.ReadUInt16();
                    uint   dosDateTime    = reader.ReadUInt32();
                    uint   crc32          = reader.ReadUInt32();
                    long   compSize       = reader.ReadUInt32();
                    long   uncompSize     = reader.ReadUInt32();
                    ushort nameLength     = reader.ReadUInt16();
                    ushort extraLength    = reader.ReadUInt16();

                    long dataOffset = _stream.Position + nameLength + extraLength;

                    byte[] nameBytes = nameLength > 0 ? reader.ReadBytes(nameLength) : null;

                    var entry = new Entry
                    {
                        Method           = (CompressionMethod)method,
                        Flags            = flags,
                        Crc32            = crc32,
                        CompressedSize   = compSize,
                        UncompressedSize = uncompSize,
                        LastWriteTime    = DosToDateTime(dosDateTime),
                        IsEncrypted      = (flags & FLAG_ENCRYPTED) != 0
                    };

                    // Parse extra fields
                    if(extraLength > 0)
                    {
                        long extraStart = _stream.Position;

                        // ZIP64 fields may override sizes
                        ParseExtraFields(reader, extraLength, ref entry);

                        _stream.Position = extraStart + extraLength;
                    }

                    // If data descriptor flag is set, we need to find the sizes after the data
                    if((flags & FLAG_DATA_DESCRIPTOR) != 0)
                    {
                        // Skip to the data descriptor after compressed data
                        // For streaming archives, scan for the next signature
                        FindDataDescriptor(reader, ref entry);
                    }

                    // Decode filename
                    if(nameBytes is not null)
                    {
                        entry.Filename = (flags & FLAG_UTF8) != 0
                                             ? Encoding.UTF8.GetString(nameBytes)
                                             : _encoding.GetString(nameBytes);

                        entry.Filename = entry.Filename.Replace('\\', '/');

                        if(entry.Filename.EndsWith('/') && entry.UncompressedSize == 0) entry.IsDirectory = true;
                    }

                    entry.DataOffset = dataOffset;

                    long nextPos = (flags & FLAG_DATA_DESCRIPTOR) != 0
                                       ? _stream.Position
                                       : dataOffset + entry.CompressedSize;

                    _entries.Add(entry);

                    _stream.Position = nextPos;

                    break;
                }

                case CENTRAL_DIR_SIG:
                    // Hit central directory — stop scanning
                    return;

                default:
                    // Try to find the next valid entry
                    if(!FindNextLocalHeader(reader)) return;

                    break;
            }
        }
    }

    void ParseExtraFields(BinaryReader reader, int length, ref Entry entry)
    {
        long end = _stream.Position + length;

        while(_stream.Position + 4 <= end)
        {
            ushort extId   = reader.ReadUInt16();
            ushort extSize = reader.ReadUInt16();

            if(_stream.Position + extSize > end) break;

            long nextExtra = _stream.Position + extSize;

            switch(extId)
            {
                case EXTRA_ZIP64:
                    if(entry.UncompressedSize == 0xFFFFFFFF && _stream.Position + 8 <= nextExtra)
                        entry.UncompressedSize = reader.ReadInt64();

                    if(entry.CompressedSize == 0xFFFFFFFF && _stream.Position + 8 <= nextExtra)
                        entry.CompressedSize = reader.ReadInt64();

                    break;

                case EXTRA_EXT_TIMESTAMP when extSize >= 1:
                {
                    byte tsFlags = reader.ReadByte();

                    if((tsFlags & 1) != 0 && _stream.Position + 4 <= nextExtra)
                        entry.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt32()).DateTime;

                    if((tsFlags & 2) != 0 && _stream.Position + 4 <= nextExtra)
                        entry.LastAccessTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt32()).DateTime;

                    if((tsFlags & 4) != 0 && _stream.Position + 4 <= nextExtra)
                        entry.CreationTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt32()).DateTime;

                    break;
                }

                case EXTRA_UNIX1 when extSize >= 8:
                {
                    entry.LastAccessTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt32()).DateTime;
                    entry.LastWriteTime  = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt32()).DateTime;

                    if(extSize >= 10) entry.Uid = reader.ReadUInt16();

                    if(extSize >= 12) entry.Gid = reader.ReadUInt16();

                    break;
                }

                case EXTRA_UNIX2 when extSize >= 4:
                {
                    entry.Uid = reader.ReadUInt16();
                    entry.Gid = reader.ReadUInt16();

                    break;
                }

                case EXTRA_UNIX3 when extSize >= 4:
                {
                    byte version = reader.ReadByte();

                    if(version == 1)
                    {
                        byte uidSize = reader.ReadByte();

                        entry.Uid = uidSize switch
                                    {
                                        2 => reader.ReadUInt16(),
                                        4 => reader.ReadUInt32(),
                                        _ => entry.Uid
                                    };

                        if(uidSize != 2 && uidSize != 4) _stream.Position += uidSize;

                        byte gidSize = reader.ReadByte();

                        entry.Gid = gidSize switch
                                    {
                                        2 => reader.ReadUInt16(),
                                        4 => reader.ReadUInt32(),
                                        _ => entry.Gid
                                    };

                        if(gidSize != 2 && gidSize != 4) _stream.Position += gidSize;
                    }

                    break;
                }

                case EXTRA_UNICODE_PATH when extSize >= 6:
                {
                    byte unicodeVersion = reader.ReadByte();

                    if(unicodeVersion == 1)
                    {
                        uint nameCrc = reader.ReadUInt32();

                        int unicodeLen = extSize - 5;

                        if(unicodeLen > 0)
                        {
                            byte[] unicodeBytes = reader.ReadBytes(unicodeLen);

                            // Strip trailing null bytes
                            int trimLen = unicodeLen;

                            while(trimLen > 0 && unicodeBytes[trimLen - 1] == 0) trimLen--;

                            if(trimLen > 0)
                            {
                                string unicodeName = Encoding.UTF8.GetString(unicodeBytes, 0, trimLen);

                                // Normalize path separators
                                unicodeName    = unicodeName.Replace('\\', '/');
                                entry.Filename = unicodeName;
                            }
                        }
                    }

                    break;
                }

                case EXTRA_WINZIP_AES when extSize >= 7:
                {
                    ushort aesVersion = reader.ReadUInt16();
                    ushort aesVendor  = reader.ReadUInt16();
                    byte   aesKeySize = reader.ReadByte();
                    ushort aesMethod  = reader.ReadUInt16();

                    // The actual compression method is stored in the AES extra field
                    if(entry.Method == CompressionMethod.WinZipAes) entry.Method = (CompressionMethod)aesMethod;

                    entry.IsEncrypted = true;

                    break;
                }
            }

            _stream.Position = nextExtra;
        }

        _stream.Position = end;
    }

    void FindDataDescriptor(BinaryReader reader, ref Entry entry)
    {
        // When the data descriptor flag is set, the CRC and sizes follow the compressed data.
        // We scan for the data descriptor signature or the next local header/central directory.
        long startPos = _stream.Position + entry.CompressedSize;

        if(startPos >= _stream.Length) return;

        _stream.Position = startPos;

        // Try to read a data descriptor at the expected position
        if(_stream.Position + 16 <= _stream.Length)
        {
            uint possibleSig = reader.ReadUInt32();

            if(possibleSig == DATA_DESCRIPTOR_SIG)
            {
                // Data descriptor with signature
                entry.Crc32            = reader.ReadUInt32();
                entry.CompressedSize   = reader.ReadUInt32();
                entry.UncompressedSize = reader.ReadUInt32();

                // Check for ZIP64 data descriptor (8-byte sizes)
                if(entry.CompressedSize == 0xFFFFFFFF || entry.UncompressedSize == 0xFFFFFFFF)
                {
                    _stream.Position       -= 8;
                    entry.CompressedSize   =  reader.ReadInt64();
                    entry.UncompressedSize =  reader.ReadInt64();
                }
            }
            else
            {
                // Data descriptor without signature (CRC directly)
                entry.Crc32            = possibleSig;
                entry.CompressedSize   = reader.ReadUInt32();
                entry.UncompressedSize = reader.ReadUInt32();
            }
        }
    }

    bool FindNextLocalHeader(BinaryReader reader)
    {
        // Scan forward for the next PK signature
        while(_stream.Position < _stream.Length - 4)
        {
            byte b = reader.ReadByte();

            if(b != 0x50) continue;

            if(_stream.Position >= _stream.Length - 3) return false;

            byte b2 = reader.ReadByte();

            if(b2 != 0x4B)
            {
                _stream.Position--;

                continue;
            }

            byte b3 = reader.ReadByte();
            byte b4 = reader.ReadByte();

            // Local header, central directory, or EOCD
            if(b3 == 0x03 && b4 == 0x04 || b3 == 0x01 && b4 == 0x02 || b3 == 0x05 && b4 == 0x06)
            {
                _stream.Position -= 4;

                return true;
            }
        }

        return false;
    }

    static DateTime DosToDateTime(uint dosDateTime)
    {
        var time = (int)(dosDateTime & 0xFFFF);
        var date = (int)(dosDateTime >> 16);

        int second = (time & 0x1F) * 2;
        int minute = time >> 5  & 0x3F;
        int hour   = time >> 11 & 0x1F;

        int day   = date      & 0x1F;
        int month = date >> 5 & 0x0F;
        int year  = (date >> 9 & 0x7F) + 1980;

        // Validate ranges
        if(month < 1) month  = 1;
        if(month > 12) month = 12;
        if(day   < 1) day    = 1;

        if(day > DateTime.DaysInMonth(year, month)) day = DateTime.DaysInMonth(year, month);

        if(hour   > 23) hour   = 23;
        if(minute > 59) minute = 59;
        if(second > 59) second = 59;

        try
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        }
        catch(ArgumentOutOfRangeException)
        {
            return default(DateTime);
        }
    }

    static uint ComputeCrc32(byte[] data)
    {
        var crc = 0xFFFFFFFF;

        foreach(byte b in data)
        {
            crc ^= b;

            for(var j = 0; j < 8; j++)
            {
                if((crc & 1) != 0)
                    crc = crc >> 1 ^ CRC_POLY;
                else
                    crc >>= 1;
            }
        }

        return crc ^ 0xFFFFFFFF;
    }

#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter filter, Encoding encoding)
    {
        if(filter is null || filter.DataForkLength < MIN_FILE_SIZE) return ErrorNumber.InvalidArgument;

        _stream   = filter.GetDataForkStream();
        _encoding = encoding ?? Encoding.GetEncoding("ibm437");
        _entries  = [];

        _features = ArchiveSupportedFeature.SupportsFilenames | ArchiveSupportedFeature.SupportsCompression;

        long eocdOffset     = -1;
        long zip64LocOffset = -1;

        FindEndOfCentralDirectory(out eocdOffset, out zip64LocOffset);

        if(eocdOffset >= 0)
            ParseCentralDirectory(eocdOffset, zip64LocOffset);
        else
            ParseWithoutCentralDirectory();

        // Detect features from parsed entries
        foreach(Entry entry in _entries)
        {
            if(entry.IsDirectory)
            {
                _features |= ArchiveSupportedFeature.SupportsSubdirectories |
                             ArchiveSupportedFeature.HasExplicitDirectories;
            }

            if(entry.LastWriteTime != default(DateTime)) _features |= ArchiveSupportedFeature.HasEntryTimestamp;

            if(entry.Comment is not null) _features |= ArchiveSupportedFeature.SupportsXAttrs;

            if(entry.IsEncrypted) _features |= ArchiveSupportedFeature.SupportsProtection;
        }

        Opened = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public void Close()
    {
        _entries = null;
        Opened   = false;
    }

#endregion
}