// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HDDVD.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     HD DVD video disc helpers.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Decryption.Aacs;

/// <summary>Aligns contiguous 2048-byte sectors to CPS units for decrypt during image conversion.</summary>
public sealed class HDDVD
{
    /// <summary>
    ///     HD DVD video discs always have a <c>HVDVD_TS</c> folder. If it doesn't have one, it's not a HD DVD video.
    /// </summary>
    /// <param name="input"><c>IOpticalMediaImage</c> to check for <c>HVDVD_TS</c> folder in.</param>
    /// <param name="fs"><c>IReadOnlyFilesystem</c> to check in.</param>
    /// <param name="partition"><c>Partition</c> to check in.</param>
    /// <returns><c>true</c> if <c>HVDVD_TS</c> folder was found.</returns>
    public static bool HasHdDvdVideoTsFolder(IOpticalMediaImage input, IReadOnlyFilesystem fs, Partition partition)
    {
        ErrorNumber error = fs.Mount(input, partition, null, null, null);

        if(error != ErrorNumber.NoError) return false;

        error = fs.Stat("HVDVD_TS", out FileEntryInfo stat);
        fs.Unmount();

        return error == ErrorNumber.NoError;
    }
}