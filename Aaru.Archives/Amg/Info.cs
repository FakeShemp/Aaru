using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Spectre.Console;

namespace Aaru.Archives;

public sealed partial class Amg
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<ArcHeader>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<ArcHeader>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        ArcHeader header = Marshal.ByteArrayToStructureLittleEndian<ArcHeader>(hdr);

        // Not a valid magic
        return header.magic == ARC_MAGIC;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        encoding    ??= Encoding.ASCII;
        information =   "";

        if(filter.DataForkLength < Marshal.SizeOf<ArcHeader>()) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<ArcHeader>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        ArcHeader header = Marshal.ByteArrayToStructureLittleEndian<ArcHeader>(hdr);

        // Not a valid magic
        if(header.magic != ARC_MAGIC) return;

        var sb = new StringBuilder();
        sb.AppendLine(Localization.AMG_archive);

        sb.AppendFormat(Localization.AMG_version_0_1, header.version >> 4, header.version & 0xF).AppendLine();

        if(header.files > 0)
            sb.AppendFormat(Localization.Archive_contains_0_files_for_1_bytes, header.files, header.size).AppendLine();

        if(header.commentLength > 0)
        {
            byte[] buffer = new byte[header.commentLength];
            stream.ReadExactly(buffer, 0, buffer.Length);
            sb.AppendLine(Localization.Archive_comment);
            sb.AppendLine(Markup.Escape(StringHandlers.CToString(buffer, encoding)));
        }

        information = sb.ToString();
    }

#endregion
}