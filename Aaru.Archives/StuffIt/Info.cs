using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class StuffIt
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

        var magic2 = BigEndianBitConverter.ToUInt32(hdr, 10);

        if(magic2 != MAGIC2) return false;

        var magic = BigEndianBitConverter.ToUInt32(hdr, 0);

        if(magic == MAGIC) return true;

        // Installer archives: "STin", "ST00"-"ST99", "ST51"
        if(hdr[0] == (byte)'S' && hdr[1] == (byte)'T')
        {
            if(hdr[2] == (byte)'i' && (hdr[3] == (byte)'n' || hdr[3] >= (byte)'0' && hdr[3] <= (byte)'9')) return true;

            if(hdr[2] >= (byte)'0' && hdr[2] <= (byte)'9' && hdr[3] >= (byte)'0' && hdr[3] <= (byte)'9') return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Identify(filter)) return;

        information = Localization.StuffIt_archive;
    }

#endregion
}