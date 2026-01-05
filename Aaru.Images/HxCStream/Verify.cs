// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Verifies HxC Stream flux images.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and it your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.IO;
using System.Linq;
using Aaru.Checksums;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class HxCStream
{
    public bool? VerifyMediaImage()
    {
        if(_trackCaptures == null || _trackCaptures.Count == 0) return null;

        if(_trackFilePaths == null || _trackFilePaths.Count == 0) return null;

        // Verify CRC32 checksums in all track files
        foreach(string trackfile in _trackFilePaths)
        {
            if(!File.Exists(trackfile)) return false;

            using FileStream fileStream = File.OpenRead(trackfile);
            byte[] fileData = new byte[fileStream.Length];
            fileStream.EnsureRead(fileData, 0, (int)fileStream.Length);

            long fileOffset = 0;

            while(fileOffset < fileData.Length)
            {
                if(fileOffset + Marshal.SizeOf<HxCStreamChunkHeader>() > fileData.Length) return false;

                HxCStreamChunkHeader chunkHeader = Marshal.ByteArrayToStructureLittleEndian<HxCStreamChunkHeader>(
                    fileData, (int)fileOffset, Marshal.SizeOf<HxCStreamChunkHeader>());

                if(!_hxcStreamSignature.SequenceEqual(chunkHeader.signature)) return false;

                if(chunkHeader.size > fileData.Length - fileOffset) return false;

                // Verify CRC32 - calculate CRC of chunk data (excluding the CRC itself)
                byte[] chunkData = new byte[chunkHeader.size - 4];
                Array.Copy(fileData, (int)fileOffset, chunkData, 0, (int)(chunkHeader.size - 4));

                uint storedCrc = BitConverter.ToUInt32(fileData, (int)(fileOffset + chunkHeader.size - 4));

                if(!VerifyChunkCrc32(chunkData, storedCrc)) return false;

                fileOffset += chunkHeader.size;
            }
        }

        // Basic verification: check that all track captures have valid data
        foreach(TrackCapture capture in _trackCaptures)
        {
            if(capture.fluxPulses == null || capture.fluxPulses.Length == 0) return false;

            if(capture.resolution == 0) return false;
        }

        return true;
    }
}