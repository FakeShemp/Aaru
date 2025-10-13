using System;
using System.Collections.Generic;
using Aaru.CommonTypes;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   sectorSize) => throw new NotImplementedException();

#endregion
}