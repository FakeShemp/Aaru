using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class CompactPro
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(hdr, 0, hdr.Length);

        // First byte must be the marker
        if(hdr[0] != MARKER) return false;

        // Bytes 4-7 are the big-endian directory offset
        var dirOffset = BigEndianBitConverter.ToUInt32(hdr, 4);

        // Directory offset must be within the file, with room for at least the header CRC + numEntries + commentLen
        if(dirOffset + 7 > (ulong)filter.DataForkLength) return false;

        return true;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < MIN_HEADER_SIZE) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(hdr, 0, hdr.Length);

        if(hdr[0] != MARKER) return;

        var sb = new StringBuilder();
        sb.AppendLine(Localization.CompactPro_archive);

        information = sb.ToString();
    }

#endregion
}