using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Aaru.Checksums;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.Helpers;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_set_dumphw(void *context, uint8_t *data, size_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_dumphw", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_dumphw(IntPtr context, [In] byte[] data, nuint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_dumphw(void *context, uint8_t *buffer, size_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_dumphw", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_dumphw(IntPtr context, byte[] buffer, ref nuint length);

#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool SetDumpHardware(List<DumpHardware> dumpHardware)
    {
        var    blockMs = new MemoryStream();
        var    dumpMs  = new MemoryStream();
        byte[] structureBytes;

        foreach(DumpHardware dump in dumpHardware)
        {
            byte[] dumpManufacturer            = null;
            byte[] dumpModel                   = null;
            byte[] dumpRevision                = null;
            byte[] dumpFirmware                = null;
            byte[] dumpSerial                  = null;
            byte[] dumpSoftwareName            = null;
            byte[] dumpSoftwareVersion         = null;
            byte[] dumpSoftwareOperatingSystem = null;

            if(!string.IsNullOrWhiteSpace(dump.Manufacturer))
                dumpManufacturer = Encoding.UTF8.GetBytes(dump.Manufacturer);

            if(!string.IsNullOrWhiteSpace(dump.Model)) dumpModel = Encoding.UTF8.GetBytes(dump.Model);

            if(!string.IsNullOrWhiteSpace(dump.Revision)) dumpRevision = Encoding.UTF8.GetBytes(dump.Revision);

            if(!string.IsNullOrWhiteSpace(dump.Firmware)) dumpFirmware = Encoding.UTF8.GetBytes(dump.Firmware);

            if(!string.IsNullOrWhiteSpace(dump.Serial)) dumpSerial = Encoding.UTF8.GetBytes(dump.Serial);

            if(!string.IsNullOrWhiteSpace(dump.Software?.Name))
                dumpSoftwareName = Encoding.UTF8.GetBytes(dump.Software.Name);

            if(!string.IsNullOrWhiteSpace(dump.Software?.Version))
                dumpSoftwareVersion = Encoding.UTF8.GetBytes(dump.Software.Version);

            if(!string.IsNullOrWhiteSpace(dump.Software?.OperatingSystem))
                dumpSoftwareOperatingSystem = Encoding.UTF8.GetBytes(dump.Software.OperatingSystem);

            var dumpEntry = new DumpHardwareEntry
            {
                ManufacturerLength            = (uint)(dumpManufacturer?.Length            + 1 ?? 0),
                ModelLength                   = (uint)(dumpModel?.Length                   + 1 ?? 0),
                RevisionLength                = (uint)(dumpRevision?.Length                + 1 ?? 0),
                FirmwareLength                = (uint)(dumpFirmware?.Length                + 1 ?? 0),
                SerialLength                  = (uint)(dumpSerial?.Length                  + 1 ?? 0),
                SoftwareNameLength            = (uint)(dumpSoftwareName?.Length            + 1 ?? 0),
                SoftwareVersionLength         = (uint)(dumpSoftwareVersion?.Length         + 1 ?? 0),
                SoftwareOperatingSystemLength = (uint)(dumpSoftwareOperatingSystem?.Length + 1 ?? 0),
                Extents                       = (uint)dump.Extents.Count
            };

            structureBytes = new byte[Marshal.SizeOf<DumpHardwareEntry>()];
            MemoryMarshal.Write(structureBytes, in dumpEntry);
            dumpMs.Write(structureBytes, 0, structureBytes.Length);

            if(dumpManufacturer != null)
            {
                dumpMs.Write(dumpManufacturer, 0, dumpManufacturer.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpModel != null)
            {
                dumpMs.Write(dumpModel, 0, dumpModel.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpRevision != null)
            {
                dumpMs.Write(dumpRevision, 0, dumpRevision.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpFirmware != null)
            {
                dumpMs.Write(dumpFirmware, 0, dumpFirmware.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpSerial != null)
            {
                dumpMs.Write(dumpSerial, 0, dumpSerial.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpSoftwareName != null)
            {
                dumpMs.Write(dumpSoftwareName, 0, dumpSoftwareName.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpSoftwareVersion != null)
            {
                dumpMs.Write(dumpSoftwareVersion, 0, dumpSoftwareVersion.Length);
                dumpMs.WriteByte(0);
            }

            if(dumpSoftwareOperatingSystem != null)
            {
                dumpMs.Write(dumpSoftwareOperatingSystem, 0, dumpSoftwareOperatingSystem.Length);
                dumpMs.WriteByte(0);
            }

            foreach(Extent extent in dump.Extents)
            {
                dumpMs.Write(BitConverter.GetBytes(extent.Start), 0, sizeof(ulong));
                dumpMs.Write(BitConverter.GetBytes(extent.End),   0, sizeof(ulong));
            }
        }

        Crc64Context.Data(dumpMs.ToArray(), out byte[] dumpCrc);

        var dumpBlock = new DumpHardwareHeader
        {
            Identifier = BlockType.DumpHardwareBlock,
            Entries    = (ushort)dumpHardware.Count,
            Crc64      = Swapping.Swap(BitConverter.ToUInt64(dumpCrc, 0)),
            Length     = (uint)dumpMs.Length
        };

        structureBytes = new byte[Marshal.SizeOf<DumpHardwareHeader>()];
        MemoryMarshal.Write(structureBytes, in dumpBlock);
        blockMs.Write(structureBytes,   0, structureBytes.Length);
        blockMs.Write(dumpMs.ToArray(), 0, (int)dumpMs.Length);

        byte[] blockBytes = blockMs.ToArray();

        Status res = aaruf_set_dumphw(_context, blockBytes, (nuint)blockBytes.Length);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }


    /// <inheritdoc />
    public List<DumpHardware> DumpHardware
    {
        get
        {
            nuint  length = 0;
            Status res    = aaruf_get_dumphw(_context, null, ref length);

            if(res != Status.Ok && res != Status.BufferTooSmall)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            var buffer = new byte[length];
            res = aaruf_get_dumphw(_context, buffer, ref length);

            if(res != Status.Ok)
            {
                ErrorMessage = StatusToErrorMessage(res);

                return null;
            }

            var memoryStream   = new MemoryStream(buffer);
            var structureBytes = new byte[Marshal.SizeOf<DumpHardwareHeader>()];
            memoryStream.EnsureRead(structureBytes, 0, structureBytes.Length);

            DumpHardwareHeader dumpBlock = Marshal.SpanToStructureLittleEndian<DumpHardwareHeader>(structureBytes);

            if(dumpBlock.Identifier != BlockType.DumpHardwareBlock) return null;

            structureBytes = new byte[dumpBlock.Length];
            memoryStream.EnsureRead(structureBytes, 0, structureBytes.Length);

            memoryStream.Position -= structureBytes.Length;

            List<DumpHardware> dumpHardware = [];

            for(ushort i = 0; i < dumpBlock.Entries; i++)
            {
                structureBytes = new byte[Marshal.SizeOf<DumpHardwareEntry>()];
                memoryStream.EnsureRead(structureBytes, 0, structureBytes.Length);

                DumpHardwareEntry dumpEntry = Marshal.SpanToStructureLittleEndian<DumpHardwareEntry>(structureBytes);

                var dump = new DumpHardware
                {
                    Software = new Software(),
                    Extents  = []
                };

                byte[] tmp;

                if(dumpEntry.ManufacturerLength > 0)
                {
                    tmp = new byte[dumpEntry.ManufacturerLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Manufacturer     =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.ModelLength > 0)
                {
                    tmp = new byte[dumpEntry.ModelLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Model            =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.RevisionLength > 0)
                {
                    tmp = new byte[dumpEntry.RevisionLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Revision         =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.FirmwareLength > 0)
                {
                    tmp = new byte[dumpEntry.FirmwareLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Firmware         =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.SerialLength > 0)
                {
                    tmp = new byte[dumpEntry.SerialLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Serial           =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.SoftwareNameLength > 0)
                {
                    tmp = new byte[dumpEntry.SoftwareNameLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Software.Name    =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.SoftwareVersionLength > 0)
                {
                    tmp = new byte[dumpEntry.SoftwareVersionLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position += 1;
                    dump.Software.Version =  Encoding.UTF8.GetString(tmp);
                }

                if(dumpEntry.SoftwareOperatingSystemLength > 0)
                {
                    tmp = new byte[dumpEntry.SoftwareOperatingSystemLength - 1];
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);
                    memoryStream.Position         += 1;
                    dump.Software.OperatingSystem =  Encoding.UTF8.GetString(tmp);
                }

                tmp = new byte[16];

                for(uint j = 0; j < dumpEntry.Extents; j++)
                {
                    memoryStream.EnsureRead(tmp, 0, tmp.Length);

                    dump.Extents.Add(new Extent
                    {
                        Start = BitConverter.ToUInt64(tmp, 0),
                        End   = BitConverter.ToUInt64(tmp, 8)
                    });
                }

                dump.Extents = dump.Extents.OrderBy(static t => t.Start).ToList();

                if(dump.Extents.Count > 0) dumpHardware.Add(dump);
            }

            if(dumpHardware.Count == 0) dumpHardware = null;

            return dumpHardware;
        }
    }

#endregion
}