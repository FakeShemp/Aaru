using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class Ha
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<HaHeader>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<HaHeader>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        HaHeader header = Marshal.ByteArrayToStructureLittleEndian<HaHeader>(hdr);

        // Not a valid magic
        return header.magic == HA_MAGIC;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < Marshal.SizeOf<HaHeader>()) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<HaHeader>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        HaHeader header = Marshal.ByteArrayToStructureLittleEndian<HaHeader>(hdr);

        // Not a valid magic
        if(header.magic != HA_MAGIC) return;

        var sb = new StringBuilder();
        sb.AppendLine(Localization.HA_archive);

        int vertype = stream.ReadByte();

        sb.AppendFormat(Localization.Created_with_HA_version_0, vertype >> 4).AppendLine();

        sb.AppendFormat(Localization.Archive_contains_0_files, header.count).AppendLine();

        information = sb.ToString();
    }

#endregion
}