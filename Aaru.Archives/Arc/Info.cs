using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Archives;

public sealed partial class Arc
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<Header>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<Header>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        Header header = Marshal.ByteArrayToStructureLittleEndian<Header>(hdr);

        // Not a valid marker
        if(header.marker != MARKER) return false;

        switch((int)header.method)
        {
            // Not a valid compression method
            case > 12 and < 20:
            // Not a valid informational item
            case > 22 and < 30:
            // Not a valid control item
            case > 31:
                return false;
        }

        for(int i = 0; i < 11; i++)

            // Not a valid filename character
            if(header.filename[i] > 0 && header.filename[i] < 0x20)
                return false;

        // If the filename is not 8.3, it's probably not an ARC file, but maybe it is in MVS/UNIX?
        if(header.filename[11] != 0) return false;

        // Compressed size is larger than file size
        // Hope for the best
        return header.compressed < stream.Length;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(filter.DataForkLength < Marshal.SizeOf<Header>()) return;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<Header>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        Header header = Marshal.ByteArrayToStructureLittleEndian<Header>(hdr);

        AaruLogging.Debug(MODULE_NAME, "[navy]header.marker[/] = [teal]0x{0:X2}[/]", header.marker);
        AaruLogging.Debug(MODULE_NAME, "[navy]header.method[/] = [teal]{0}[/]",      header.method);

        AaruLogging.Debug(MODULE_NAME,
                          "[navy]header.filename[/] = [green]\"{0}\"[/]",
                          StringHandlers.CToString(header.filename));

        AaruLogging.Debug(MODULE_NAME, "[navy]header.compressed[/] = [teal]{0}[/]",   header.compressed);
        AaruLogging.Debug(MODULE_NAME, "[navy]header.date[/] = [teal]{0}[/]",         header.date);
        AaruLogging.Debug(MODULE_NAME, "[navy]header.time[/] = [teal]{0}[/]",         header.time);
        AaruLogging.Debug(MODULE_NAME, "[navy]header.crc[/] = [teal]0x{0:X4}[/]",     header.crc);
        AaruLogging.Debug(MODULE_NAME, "[navy]header.uncompressed[/] = [teal]{0}[/]", header.uncompressed);

        // Not a valid marker
        if(header.marker != MARKER) return;

        switch((int)header.method)
        {
            // Not a valid compression method
            case > 12 and < 20:
            // Not a valid informational item
            case > 22 and < 30:
            // Not a valid control item
            case > 31:
                return;
        }

        for(int i = 0; i < 11; i++)

            // Not a valid filename character
            if(header.filename[i] > 0 && header.filename[i] < 0x20)
                return;

        // If the filename is not 8.3, it's probably not an ARC file, but maybe it is in MVS/UNIX?
        if(header.filename[11] != 0) return;

        // Compressed size is larger than file size
        if(header.compressed >= stream.Length) return;

        // Hope for the best

        var sb = new StringBuilder();
        sb.AppendLine("[bold][blue]ARC archive[/][/]");

        information = sb.ToString();
    }

#endregion
}