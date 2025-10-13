using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using TapePartition = Aaru.CommonTypes.Structs.TapePartition;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   sectorSize) => throw new NotImplementedException();

#endregion

#region IWritableTapeImage Members

    /// <inheritdoc />
    public List<TapePartition> TapePartitions { get; }

#endregion
}