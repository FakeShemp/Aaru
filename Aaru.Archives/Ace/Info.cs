using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Ace
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_MAIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();

        return FindSignature(stream) >= 0;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < MIN_MAIN_HEADER_SIZE) return;

        Stream stream = filter.GetDataForkStream();
        long   sigPos = FindSignature(stream);

        if(sigPos < 0) return;

        long headerStart = sigPos - SIGNATURE_HEADER_OFFSET;

        if(headerStart < 0) return;

        stream.Position = headerStart;

        // Read CRC(2) + SIZE(2)
        var prefix = new byte[4];

        if(stream.Read(prefix, 0, 4) < 4) return;

        var headerSize = BitConverter.ToUInt16(prefix, 2);

        if(headerSize < 27 || headerSize > 32768) return;

        var headerData = new byte[headerSize];

        if(stream.Read(headerData, 0, headerSize) < headerSize) return;

        if(headerData[0] != (byte)HeaderType.Main) return;

        var  mainFlags  = BitConverter.ToUInt16(headerData, 1);
        byte verExtract = headerData[10];
        byte verCreated = headerData[11];
        var  host       = (HostOs)headerData[12];
        byte volNum     = headerData[13];

        var sb = new StringBuilder();
        sb.AppendLine(Localization.ACE_archive);

        sb.AppendFormat(Localization.ACE_version_created_0_1, verCreated / 10, verCreated % 10).AppendLine();

        sb.AppendFormat(Localization.ACE_version_needed_0_1, verExtract / 10, verExtract % 10).AppendLine();

        sb.AppendFormat(Localization.ACE_host_os_0, host).AppendLine();

        if((mainFlags & FLAG_SOLID)       != 0) sb.AppendLine(Localization.ACE_solid_archive);
        if((mainFlags & FLAG_MULTIVOLUME) != 0) sb.AppendLine(Localization.ACE_multi_volume_archive);
        if((mainFlags & FLAG_LOCKED)      != 0) sb.AppendLine(Localization.ACE_locked_archive);
        if((mainFlags & FLAG_SFX)         != 0) sb.AppendLine(Localization.ACE_sfx_archive);
        if((mainFlags & FLAG_RECOVERY)    != 0) sb.AppendLine(Localization.ACE_has_recovery_record);

        if((mainFlags & FLAG_MULTIVOLUME) != 0) sb.AppendFormat(Localization.ACE_volume_number_0, volNum).AppendLine();

        information = sb.ToString();
    }

#endregion
}