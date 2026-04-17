using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class StuffItX
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[8];
        stream.ReadExactly(hdr, 0, hdr.Length);

        for(var i = 0; i < 7; i++)
            if(hdr[i] != _signature[i])
                return false;

        // Accept only '!' (0x21), reject '?' (base-N encoded, not supported)
        return hdr[7] == (byte)'!';
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Identify(filter)) return;

        information = Localization.StuffItX_archive;
    }

#endregion
}