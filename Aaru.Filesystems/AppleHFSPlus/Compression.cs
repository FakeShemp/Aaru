// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Compression.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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
using System.IO.Compression;
using Aaru.CommonTypes.Enums;
using Aaru.Compression;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements HFS+ transparent decompression support</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>Checks if a file is compressed</summary>
    /// <param name="fileEntry">File entry to check</param>
    /// <param name="compressionType">Output compression type if compressed</param>
    /// <param name="uncompressedSize">Output uncompressed size if compressed</param>
    /// <returns>True if file is compressed</returns>
    private bool IsFileCompressed(FileEntry fileEntry, out HFSCompressionType compressionType,
                                  out ulong uncompressedSize)
    {
        compressionType  = 0;
        uncompressedSize = 0;

        // Check if the file has the decmpfs extended attribute
        ErrorNumber err = GetAttributeFromBTree(fileEntry.CNID, DECMPFS_XATTR_NAME, out byte[] decmpfsData);

        if(err != ErrorNumber.NoError || decmpfsData == null || decmpfsData.Length < 16) return false;

        // Parse the decmpfs header
        var magic = BigEndianBitConverter.ToUInt32(decmpfsData, 0);

        if(magic != DECMPFS_MAGIC) return false;

        compressionType  = (HFSCompressionType)BigEndianBitConverter.ToUInt32(decmpfsData, 4);
        uncompressedSize = BigEndianBitConverter.ToUInt64(decmpfsData, 8);

        return true;
    }

    /// <summary>Decompresses a compressed file</summary>
    /// <param name="fileEntry">File entry to decompress</param>
    /// <param name="buf">Output buffer with decompressed data</param>
    /// <returns>Error number</returns>
    private ErrorNumber DecompressFile(FileEntry fileEntry, out byte[] buf)
    {
        buf = null;

        // Get compression information
        if(!IsFileCompressed(fileEntry, out HFSCompressionType compressionType, out ulong uncompressedSize))
            return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME,
                          "DecompressFile: Decompressing file CNID={0}, type={1}, uncompressed size={2}",
                          fileEntry.CNID,
                          compressionType,
                          uncompressedSize);

        // Get the decmpfs xattr data
        ErrorNumber err = GetAttributeFromBTree(fileEntry.CNID, DECMPFS_XATTR_NAME, out byte[] decmpfsData);

        if(err != ErrorNumber.NoError || decmpfsData == null) return err;

        switch(compressionType)
        {
            case HFSCompressionType.Uncompressed:
                // Type 1: Uncompressed data stored inline in xattr
                return DecompressType1(decmpfsData, uncompressedSize, out buf);

            case HFSCompressionType.ZlibInline:
                // Type 3: ZLIB compressed data stored inline in xattr
                return DecompressType3(decmpfsData, uncompressedSize, out buf);

            case HFSCompressionType.LzvnInline:
                // Type 4: LZVN compressed data stored inline in xattr
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressFile: LZVN inline decompression not yet implemented for CNID {0}",
                                  fileEntry.CNID);

                return ErrorNumber.NotSupported;

            case HFSCompressionType.LzvnResourceFork:
            case HFSCompressionType.ZlibResourceFork:
            case HFSCompressionType.Lzvn2ResourceFork:
            case HFSCompressionType.LzfseResourceFork:
                // Types 7-10: Compressed data stored in resource fork
                return DecompressResourceFork(fileEntry, compressionType, uncompressedSize, out buf);

            default:
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressFile: Unknown compression type {0} for CNID {1}",
                                  compressionType,
                                  fileEntry.CNID);

                return ErrorNumber.NotSupported;
        }
    }

    /// <summary>Decompresses type 1 (uncompressed inline)</summary>
    private ErrorNumber DecompressType1(byte[] decmpfsData, ulong uncompressedSize, out byte[] buf)
    {
        buf = null;

        // Type 1: Uncompressed data follows the 16-byte header
        if(decmpfsData.Length < 16) return ErrorNumber.InvalidArgument;

        var dataSize = decmpfsData.Length - 16;

        if(dataSize != (int)uncompressedSize)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "DecompressType1: Size mismatch - xattr data size {0} != uncompressed size {1}",
                              dataSize,
                              uncompressedSize);
        }

        buf = new byte[uncompressedSize];
        Array.Copy(decmpfsData, 16, buf, 0, Math.Min(dataSize, (int)uncompressedSize));

        return ErrorNumber.NoError;
    }

    /// <summary>Decompresses type 3 (ZLIB inline)</summary>
    private ErrorNumber DecompressType3(byte[] decmpfsData, ulong uncompressedSize, out byte[] buf)
    {
        buf = null;

        // Type 3: ZLIB compressed data follows the 16-byte header
        if(decmpfsData.Length < 16) return ErrorNumber.InvalidArgument;

        int compressedSize = decmpfsData.Length - 16;

        try
        {
            buf = new byte[uncompressedSize];

            using var compressedStream = new MemoryStream(decmpfsData, 16, compressedSize);
            using var zlibStream       = new DeflateStream(compressedStream, CompressionMode.Decompress);

            var totalRead = 0;

            while(totalRead < (int)uncompressedSize)
            {
                int read = zlibStream.Read(buf, totalRead, (int)uncompressedSize - totalRead);

                if(read == 0) break;

                totalRead += read;
            }

            if(totalRead != (int)uncompressedSize)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressType3: Decompressed size {0} != expected size {1}",
                                  totalRead,
                                  uncompressedSize);
            }

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "DecompressType3: Exception - {0}", ex.Message);

            return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>Decompresses types 7-10 (resource fork based)</summary>
    private ErrorNumber DecompressResourceFork(FileEntry fileEntry,        HFSCompressionType compressionType,
                                               ulong     uncompressedSize, out byte[]         buf)
    {
        buf = null;

        // Read the resource fork
        ErrorNumber err = ReadResourceFork(fileEntry, out byte[] resourceForkData);

        if(err                     != ErrorNumber.NoError ||
           resourceForkData        == null                ||
           resourceForkData.Length < DECMPFS_RESOURCE_HEADER_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "DecompressResourceFork: Failed to read resource fork for CNID {0}: {1}",
                              fileEntry.CNID,
                              err);

            return err;
        }

        // Parse resource fork header
        var headerSize = BigEndianBitConverter.ToUInt32(resourceForkData, 0);
        var totalSize  = BigEndianBitConverter.ToUInt32(resourceForkData, 4);
        var dataSize   = BigEndianBitConverter.ToUInt32(resourceForkData, 8);

        if(headerSize != DECMPFS_RESOURCE_HEADER_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "DecompressResourceFork: Invalid header size {0} for CNID {1}",
                              headerSize,
                              fileEntry.CNID);

            return ErrorNumber.InvalidArgument;
        }

        // Compressed data starts after the header
        var compressedDataOffset = (int)headerSize;

        if(compressedDataOffset + dataSize > resourceForkData.Length)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "DecompressResourceFork: Compressed data size {0} exceeds resource fork size {1} for CNID {2}",
                              dataSize,
                              resourceForkData.Length,
                              fileEntry.CNID);

            return ErrorNumber.InvalidArgument;
        }

        // Decompress based on type
        switch(compressionType)
        {
            case HFSCompressionType.ZlibResourceFork:
                return DecompressZlibResourceFork(resourceForkData,
                                                  compressedDataOffset,
                                                  (int)dataSize,
                                                  uncompressedSize,
                                                  out buf);

            case HFSCompressionType.LzfseResourceFork:
                return DecompressLzfseResourceFork(resourceForkData,
                                                   compressedDataOffset,
                                                   (int)dataSize,
                                                   uncompressedSize,
                                                   out buf);

            case HFSCompressionType.LzvnResourceFork:
            case HFSCompressionType.Lzvn2ResourceFork:
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressResourceFork: LZVN resource fork decompression not yet implemented for CNID {0}",
                                  fileEntry.CNID);

                return ErrorNumber.NotSupported;

            default:
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressResourceFork: Unsupported compression type {0} for CNID {1}",
                                  compressionType,
                                  fileEntry.CNID);

                return ErrorNumber.NotSupported;
        }
    }

    /// <summary>Decompresses ZLIB compressed data from resource fork</summary>
    private ErrorNumber DecompressZlibResourceFork(byte[] resourceForkData, int        offset, int compressedSize,
                                                   ulong  uncompressedSize, out byte[] buf)
    {
        buf = null;

        try
        {
            buf = new byte[uncompressedSize];

            using var compressedStream = new MemoryStream(resourceForkData, offset, compressedSize);
            using var zlibStream       = new DeflateStream(compressedStream, CompressionMode.Decompress);

            var totalRead = 0;

            while(totalRead < (int)uncompressedSize)
            {
                int read = zlibStream.Read(buf, totalRead, (int)uncompressedSize - totalRead);

                if(read == 0) break;

                totalRead += read;
            }

            if(totalRead != (int)uncompressedSize)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressZlibResourceFork: Decompressed size {0} != expected size {1}",
                                  totalRead,
                                  uncompressedSize);
            }

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "DecompressZlibResourceFork: Exception - {0}", ex.Message);

            return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>Decompresses LZFSE compressed data from resource fork</summary>
    private static ErrorNumber DecompressLzfseResourceFork(byte[] resourceForkData, int offset, int compressedSize,
                                                           ulong  uncompressedSize, out byte[] buf)
    {
        buf = null;

        try
        {
            var compressedData = new byte[compressedSize];
            Array.Copy(resourceForkData, offset, compressedData, 0, compressedSize);

            // Use Aaru.Compression.LZFSE to decompress
            buf = new byte[uncompressedSize];
            int decompressedSize = LZFSE.DecodeBuffer(compressedData, buf);

            if(decompressedSize <= 0)
            {
                AaruLogging.Debug(MODULE_NAME, "DecompressLzfseResourceFork: LZFSE decompression failed");

                return ErrorNumber.InvalidArgument;
            }

            if(decompressedSize != (int)uncompressedSize)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "DecompressLzfseResourceFork: Decompressed size {0} != expected size {1}",
                                  decompressedSize,
                                  uncompressedSize);
            }

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "DecompressLzfseResourceFork: Exception - {0}", ex.Message);

            return ErrorNumber.InvalidArgument;
        }
    }
}