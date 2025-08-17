// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Command.cs
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Tests.Devices;

static partial class MainClass
{
    public static void Command(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_a_SCSI_command);
            AaruLogging.WriteLine(Localization.Send_an_ATA_command);
            AaruLogging.WriteLine(Localization.Send_a_SecureDigital_MultiMediaCard_command);
            AaruLogging.WriteLine(Localization.Send_a_NVMe_command);

            if(dev.ScsiType == PeripheralDeviceTypes.MultiMediaDevice)
                AaruLogging.WriteLine(Localization.Send_a_special_sequence_of_commands_for_SCSI_Multimedia_devices);

            AaruLogging.WriteLine(Localization.Return_to_device_menu);
            AaruLogging.Write(Localization.Choose);

            string strDev = Console.ReadLine();

            if(!int.TryParse(strDev, out int item))
            {
                AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                Console.ReadKey();

                continue;
            }

            switch(item)
            {
                case 0:
                    AaruLogging.WriteLine(Localization.Returning_to_device_menu);

                    return;
                case 1:
                    Scsi(devPath, dev);

                    continue;
                case 2:
                    Ata(devPath, dev);

                    continue;
                case 3:
                    SecureDigital(devPath, dev);

                    continue;
                case 4:
                    NVMe(devPath, dev);

                    continue;
                case 5 when dev.ScsiType == PeripheralDeviceTypes.MultiMediaDevice:
                    ScsiMmc.Menu(devPath, dev);

                    continue;

                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }
}