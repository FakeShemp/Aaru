using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Rar
{
    void GetRar4Information(Stream stream, StringBuilder sb)
    {
        // Stream is positioned after the 7-byte marker.
        // Read archive header block.
        var baseHeader = new byte[7];

        if(stream.Read(baseHeader, 0, 7) < 7) return;

        var flags      = BitConverter.ToUInt16(baseHeader, 3);
        var headerSize = BitConverter.ToUInt16(baseHeader, 5);

        if((flags & MHD_SOLID) != 0) sb.AppendLine(Localization.RAR_solid_archive);

        if((flags & MHD_VOLUME) != 0) sb.AppendLine(Localization.RAR_multi_volume_archive);

        if((flags & MHD_LOCK) != 0) sb.AppendLine(Localization.RAR_locked_archive);

        if((flags & MHD_PASSWORD) != 0) sb.AppendLine(Localization.RAR_encrypted_archive);

        if((flags & MHD_PROTECT) != 0) sb.AppendLine(Localization.RAR_has_recovery_record);
    }

    void GetRar5Information(Stream stream, StringBuilder sb)
    {
        // Stream is positioned after the 8-byte signature.
        // Read the first block (should be the main archive header).

        // CRC32 (4 bytes)
        var crcBuf = new byte[4];

        if(stream.Read(crcBuf, 0, 4) < 4) return;

        ulong headerSize = ReadVint(stream);
        ulong blockType  = ReadVint(stream);

        if(blockType != (ulong)Rar5BlockType.Main) return;

        ulong blockFlags = ReadVint(stream);
        ulong archFlags  = ReadVint(stream);

        if((archFlags & RAR5_ARCHIVE_SOLID) != 0) sb.AppendLine(Localization.RAR_solid_archive);

        if((archFlags & RAR5_ARCHIVE_VOLUME) != 0) sb.AppendLine(Localization.RAR_multi_volume_archive);

        if((archFlags & RAR5_ARCHIVE_LOCKED) != 0) sb.AppendLine(Localization.RAR_locked_archive);

        if((archFlags & RAR5_ARCHIVE_RECOVERY) != 0) sb.AppendLine(Localization.RAR_has_recovery_record);
    }

#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[8];
        int read   = stream.Read(header, 0, 8);

        if(read < MIN_HEADER_SIZE) return false;

        // Check RAR 5.0 signature (8 bytes)
        if(read      >= 8                 &&
           header[0] == RAR5_SIGNATURE[0] &&
           header[1] == RAR5_SIGNATURE[1] &&
           header[2] == RAR5_SIGNATURE[2] &&
           header[3] == RAR5_SIGNATURE[3] &&
           header[4] == RAR5_SIGNATURE[4] &&
           header[5] == RAR5_SIGNATURE[5] &&
           header[6] == RAR5_SIGNATURE[6] &&
           header[7] == RAR5_SIGNATURE[7])
            return true;

        // Check RAR 1.x-4.x signature (7 bytes)
        if(header[0] == RAR4_SIGNATURE[0] &&
           header[1] == RAR4_SIGNATURE[1] &&
           header[2] == RAR4_SIGNATURE[2] &&
           header[3] == RAR4_SIGNATURE[3] &&
           header[4] == RAR4_SIGNATURE[4] &&
           header[5] == RAR4_SIGNATURE[5] &&
           header[6] == RAR4_SIGNATURE[6])
            return true;

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < MIN_HEADER_SIZE) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[8];
        int read   = stream.Read(header, 0, 8);

        if(read < MIN_HEADER_SIZE) return;

        bool isRar5 = read      >= 8                 &&
                      header[0] == RAR5_SIGNATURE[0] &&
                      header[1] == RAR5_SIGNATURE[1] &&
                      header[2] == RAR5_SIGNATURE[2] &&
                      header[3] == RAR5_SIGNATURE[3] &&
                      header[4] == RAR5_SIGNATURE[4] &&
                      header[5] == RAR5_SIGNATURE[5] &&
                      header[6] == RAR5_SIGNATURE[6] &&
                      header[7] == RAR5_SIGNATURE[7];

        bool isRar4 = header[0] == RAR4_SIGNATURE[0] &&
                      header[1] == RAR4_SIGNATURE[1] &&
                      header[2] == RAR4_SIGNATURE[2] &&
                      header[3] == RAR4_SIGNATURE[3] &&
                      header[4] == RAR4_SIGNATURE[4] &&
                      header[5] == RAR4_SIGNATURE[5] &&
                      header[6] == RAR4_SIGNATURE[6];

        if(!isRar5 && !isRar4) return;

        var sb = new StringBuilder();

        if(isRar5)
        {
            sb.AppendLine(Localization.RAR_5_archive);
            GetRar5Information(stream, sb);
        }
        else
        {
            sb.AppendLine(Localization.RAR_archive);
            GetRar4Information(stream, sb);
        }

        information = sb.ToString();
    }

#endregion
}