using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class DiskDoubler
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < 62) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[4];
        stream.ReadExactly(hdr, 0, 4);

        var magic = BigEndianBitConverter.ToUInt32(hdr, 0);

        switch(magic)
        {
            case MAGIC_SINGLE when filter.DataForkLength >= 84:
            case MAGIC_DDAR when filter.DataForkLength   >= 78:
            case MAGIC_DDA2:
                return true;
            default:
                return false;
        }
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Identify(filter)) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[4];
        stream.ReadExactly(hdr, 0, 4);

        var magic = BigEndianBitConverter.ToUInt32(hdr, 0);

        StringBuilder sb = new();

        sb.AppendLine(Localization.DiskDoubler_archive);

        information = sb.ToString();
    }

#endregion
}