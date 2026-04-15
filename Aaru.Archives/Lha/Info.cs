using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;

namespace Aaru.Archives;

public sealed partial class Lha
{
    static bool IsValidMethod(byte[] data, int offset)
    {
        if(offset + METHOD_LEN > data.Length) return false;

        // Must start and end with '-'
        if(data[offset] != METHOD_DASH || data[offset + 4] != METHOD_DASH) return false;

        byte family1 = data[offset + 1];
        byte family2 = data[offset + 2];
        byte method  = data[offset + 3];

        // LHA/LZH family: -lhN- where N is 0-7 or 'd'
        if(family1 == (byte)'l' && family2 == (byte)'h') return method is >= (byte)'0' and <= (byte)'7' or (byte)'d';

        // LARC family: -lzN- where N is 0, 4, 5, or 's'
        if(family1 == (byte)'l' && family2 == (byte)'z')
            return method is (byte)'0' or (byte)'4' or (byte)'5' or (byte)'s';

        // PMARC family: -pmN- where N is 0, 1, 2
        if(family1 == (byte)'p' && family2 == (byte)'m') return method is >= (byte)'0' and <= (byte)'2';

        return false;
    }

#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(hdr, 0, hdr.Length);

        // Method field is at bytes 2-6 in all header levels
        // Format: -XX?- where XX is lh/lz/pm and ? is the method character
        return IsValidMethod(hdr, 2);
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

        if(!IsValidMethod(hdr, 2)) return;

        AaruLogging.Debug(MODULE_NAME,
                          "[navy]method[/] = [green]\"{0}\"[/]",
                          Encoding.ASCII.GetString(hdr, 2, METHOD_LEN));

        var sb = new StringBuilder();
        sb.AppendLine(Localization.LHA_archive);

        information = sb.ToString();
    }

#endregion
}