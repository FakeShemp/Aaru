using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class StuffIt5
{
    /// <summary>
    ///     Header signature pattern. Bytes with value 0xFF are wildcards (year digits that vary).
    /// </summary>
    static readonly byte[] _headerPattern =
    {
        (byte)'S', (byte)'t', (byte)'u', (byte)'f', (byte)'f', (byte)'I', (byte)'t', (byte)' ', (byte)'(', (byte)'c',
        (byte)')', (byte)'1', (byte)'9', (byte)'9', (byte)'7', (byte)'-', 0xFF, 0xFF, 0xFF, 0xFF, (byte)' ', (byte)'A',
        (byte)'l', (byte)'a', (byte)'d', (byte)'d', (byte)'i', (byte)'n', (byte)' ', (byte)'S', (byte)'y', (byte)'s',
        (byte)'t', (byte)'e', (byte)'m', (byte)'s', (byte)',', (byte)' ', (byte)'I', (byte)'n', (byte)'c', (byte)'.',
        (byte)',', (byte)' ', (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'w',
        (byte)'w', (byte)'w', (byte)'.', (byte)'a', (byte)'l', (byte)'a', (byte)'d', (byte)'d', (byte)'i', (byte)'n',
        (byte)'s', (byte)'y', (byte)'s', (byte)'.', (byte)'c', (byte)'o', (byte)'m', (byte)'/', (byte)'S', (byte)'t',
        (byte)'u', (byte)'f', (byte)'f', (byte)'I', (byte)'t', (byte)'/', (byte)'\r', (byte)'\n'
    };

#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var hdr = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(hdr, 0, hdr.Length);

        // Match the header pattern (0xFF bytes are wildcards for year)
        for(var i = 0; i < _headerPattern.Length; i++)
        {
            if(_headerPattern[i] == 0xFF) continue;

            if(hdr[i] != _headerPattern[i]) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Identify(filter)) return;

        information = Localization.StuffIt5_archive;
    }

#endregion
}