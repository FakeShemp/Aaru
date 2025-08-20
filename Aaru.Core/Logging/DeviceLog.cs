// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceLog.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains glue logic for writing a dump log.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.IO;
using Aaru.CommonTypes.Interop;
using Aaru.Devices;
using Aaru.Logging;
using Sentry;

namespace Aaru.Core.Logging;

/// <summary>Creates a dump log</summary>
public static class DeviceLog
{
    /// <summary>Initializes the dump log</summary>
    /// <param name="dev">Device</param>
    /// <param name="private">Disable saving paths or serial numbers in log</param>
    public static void StartLog(Device dev, bool @private)
    {
        if(@private)
        {
            string[] args = Environment.GetCommandLineArgs();

            for(int i = 0; i < args.Length; i++)
            {
                if(args[i].StartsWith("/dev",    StringComparison.OrdinalIgnoreCase) ||
                   args[i].StartsWith("aaru://", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    args[i] = Path.GetFileName(args[i]);
                }
                catch(Exception ex)
                {
                    // Do nothing
                    SentrySdk.CaptureException(ex);
                }
            }

            AaruLogging.Information(Localization.Core.Command_line_0, string.Join(" ", args));
        }
        else
            AaruLogging.Information(Localization.Core.Command_line_0, Environment.CommandLine);


        if(dev is Aaru.Devices.Remote.Device remoteDev)
        {
            AaruLogging.Information(Localization.Core.Remote_information);
            AaruLogging.Information(Localization.Core.Server_0,  remoteDev.RemoteApplication);
            AaruLogging.Information(Localization.Core.Version_0, remoteDev.RemoteVersion);

            AaruLogging.Information(Localization.Core.Operating_system_0_1,
                                    remoteDev.RemoteOperatingSystem,
                                    remoteDev.RemoteOperatingSystemVersion);

            AaruLogging.Information(Localization.Core.Architecture_0,     remoteDev.RemoteArchitecture);
            AaruLogging.Information(Localization.Core.Protocol_version_0, remoteDev.RemoteProtocolVersion);

            AaruLogging.Information(DetectOS.IsAdmin
                                        ? Localization.Core.Running_as_superuser_Yes
                                        : Localization.Core.Running_as_superuser_No);

            AaruLogging.Information(Localization.Core.Log_section_separator);
        }

        AaruLogging.Information(Localization.Core.Device_information);
        AaruLogging.Information(Localization.Core.Manufacturer_0,      dev.Manufacturer);
        AaruLogging.Information(Localization.Core.Model_0,             dev.Model);
        AaruLogging.Information(Localization.Core.Firmware_revision_0, dev.FirmwareRevision);

        if(!@private) AaruLogging.Information(Localization.Core.Serial_number_0, dev.Serial);

        AaruLogging.Information(Localization.Core.Removable_device_0,    dev.IsRemovable);
        AaruLogging.Information(Localization.Core.Device_type_0,         dev.Type);
        AaruLogging.Information(Localization.Core.CompactFlash_device_0, dev.IsCompactFlash);
        AaruLogging.Information(Localization.Core.PCMCIA_device_0,       dev.IsPcmcia);
        AaruLogging.Information(Localization.Core.USB_device_0,          dev.IsUsb);

        if(dev.IsUsb)
        {
            AaruLogging.Information(Localization.Core.USB_manufacturer_0, dev.UsbManufacturerString);
            AaruLogging.Information(Localization.Core.USB_product_0,      dev.UsbProductString);

            if(!@private) AaruLogging.Information(Localization.Core.USB_serial_0, dev.UsbSerialString);

            AaruLogging.Information(Localization.Core.USB_vendor_ID_0,  dev.UsbVendorId);
            AaruLogging.Information(Localization.Core.USB_product_ID_0, dev.UsbProductId);
        }

        AaruLogging.Information(Localization.Core.FireWire_device_0, dev.IsFireWire);

        if(dev.IsFireWire)
        {
            AaruLogging.Information(Localization.Core.FireWire_vendor_0, dev.FireWireVendorName);
            AaruLogging.Information(Localization.Core.FireWire_model_0,  dev.FireWireModelName);

            if(!@private) AaruLogging.Information(Localization.Core.FireWire_GUID_0, dev.FireWireGuid);

            AaruLogging.Information(Localization.Core.FireWire_vendor_ID_0,  dev.FireWireVendor);
            AaruLogging.Information(Localization.Core.FireWire_product_ID_0, dev.FireWireModel);
        }

        AaruLogging.Information(Localization.Core.Log_section_separator);

        AaruLogging.Information(Localization.Core.Dumping_progress_log);
    }
}