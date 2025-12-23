// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Device.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru device testing.
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices;

static partial class MainClass
{
    public static void Device(string devPath)
    {
        AaruLogging.WriteLine("Going to open {0}. Press any key to continue...", devPath);
        Console.ReadKey();

        var dev = Aaru.Devices.Device.Create(devPath, out _);

        while(true)
        {
            AaruLogging.WriteLine("dev.PlatformID = {0}",        dev.PlatformId);
            AaruLogging.WriteLine("dev.Timeout = {0}",           dev.Timeout);
            AaruLogging.WriteLine("dev.Error = {0}",             dev.Error);
            AaruLogging.WriteLine("dev.LastError = {0}",         dev.LastError);
            AaruLogging.WriteLine("dev.Type = {0}",              dev.Type);
            AaruLogging.WriteLine("dev.Manufacturer = \"{0}\"",  dev.Manufacturer);
            AaruLogging.WriteLine("dev.Model = \"{0}\"",         dev.Model);
            AaruLogging.WriteLine("dev.Revision = \"{0}\"",      dev.FirmwareRevision);
            AaruLogging.WriteLine("dev.Serial = \"{0}\"",        dev.Serial);
            AaruLogging.WriteLine("dev.SCSIType = {0}",          dev.ScsiType);
            AaruLogging.WriteLine("dev.IsRemovable = {0}",       dev.IsRemovable);
            AaruLogging.WriteLine("dev.IsUSB = {0}",             dev.IsUsb);
            AaruLogging.WriteLine("dev.USBVendorID = 0x{0:X4}",  dev.UsbVendorId);
            AaruLogging.WriteLine("dev.USBProductID = 0x{0:X4}", dev.UsbProductId);

            AaruLogging.WriteLine("dev.USBDescriptors.Length = {0}",
                                  dev.UsbDescriptors?.Length.ToString() ?? Localization._null);

            AaruLogging.WriteLine("dev.USBManufacturerString = \"{0}\"", dev.UsbManufacturerString);
            AaruLogging.WriteLine("dev.USBProductString = \"{0}\"", dev.UsbProductString);
            AaruLogging.WriteLine("dev.USBSerialString = \"{0}\"", dev.UsbSerialString);
            AaruLogging.WriteLine("dev.IsFireWire = {0}", dev.IsFireWire);
            AaruLogging.WriteLine("dev.FireWireGUID = {0:X16}", dev.FireWireGuid);
            AaruLogging.WriteLine("dev.FireWireModel = 0x{0:X8}", dev.FireWireModel);
            AaruLogging.WriteLine("dev.FireWireModelName = \"{0}\"", dev.FireWireModelName);
            AaruLogging.WriteLine("dev.FireWireVendor = 0x{0:X8}", dev.FireWireVendor);
            AaruLogging.WriteLine("dev.FireWireVendorName = \"{0}\"", dev.FireWireVendorName);
            AaruLogging.WriteLine("dev.IsCompactFlash = {0}", dev.IsCompactFlash);
            AaruLogging.WriteLine("dev.IsPCMCIA = {0}", dev.IsPcmcia);
            AaruLogging.WriteLine("dev.CIS.Length = {0}", dev.Cis?.Length.ToString() ?? Localization._null);

            AaruLogging.WriteLine(Localization.Press_any_key_to_continue, devPath);
            Console.ReadKey();

        menu:
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Options);
            AaruLogging.WriteLine(Localization.Print_USB_descriptors);
            AaruLogging.WriteLine(Localization.Print_PCMCIA_CIS);
            AaruLogging.WriteLine(Localization._3_Send_a_command_to_the_device);
            AaruLogging.WriteLine(Localization.Return_to_device_selection);
            AaruLogging.Write(Localization.Choose);

            string strDev = Console.ReadLine();

            if(!int.TryParse(strDev, out int item))
            {
                AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                Console.ReadKey();

                goto menu;
            }

            switch(item)
            {
                case 0:
                    AaruLogging.WriteLine(Localization.Returning_to_device_selection);

                    return;
                case 1:
                    Console.Clear();
                    AaruLogging.WriteLine(Localization.Device_0, devPath);
                    AaruLogging.WriteLine(Localization.USB_descriptors);

                    if(dev.UsbDescriptors != null) PrintHex.PrintHexArray(dev.UsbDescriptors, 64);

                    AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                    Console.ReadKey();

                    goto menu;
                case 2:
                    Console.Clear();
                    AaruLogging.WriteLine(Localization.Device_0, devPath);
                    AaruLogging.WriteLine(Localization.PCMCIA_CIS);

                    if(dev.Cis != null) PrintHex.PrintHexArray(dev.Cis, 64);

                    AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                    Console.ReadKey();

                    goto menu;
                case 3:
                    Command(devPath, dev);

                    goto menu;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    goto menu;
            }
        }
    }
}