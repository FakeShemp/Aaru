using System;
using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class Stfs
{
    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<RemotePackage>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<RemotePackage>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        RemotePackage header = Marshal.ByteArrayToStructureBigEndian<RemotePackage>(hdr);

        return header.Magic is PackageMagic.Console or PackageMagic.Live or PackageMagic.Microsoft;
    }
}