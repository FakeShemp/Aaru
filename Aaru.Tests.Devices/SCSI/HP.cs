// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HP.cs
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
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.SCSI;

static class Hp
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_Hewlett_Packard_vendor_command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_READ_LONG_command);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_commands_menu);
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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_commands_menu);

                    return;
                case 1:
                    ReadLong(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void ReadLong(string devPath, Device dev)
    {
        var    relative    = false;
        uint   address     = 0;
        ushort length      = 1;
        ushort bps         = 512;
        var    physical    = false;
        var    sectorCount = true;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_LONG_command);

            AaruLogging.WriteLine(physical
                                      ? Localization.Physical_Block_Address_0
                                      : Localization.Logical_Block_Address_0,
                                  address);

            AaruLogging.WriteLine(Localization.Relative_0, relative);

            AaruLogging.WriteLine(sectorCount
                                      ? Localization.Will_transfer_0_sectors
                                      : Localization.Will_transfer_0_bytes,
                                  length);

            if(sectorCount) AaruLogging.WriteLine(Localization.Expected_sector_size_0_bytes, bps);

            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Hewlett_Packard_vendor_commands_menu);

            strDev = Console.ReadLine();

            if(!int.TryParse(strDev, out item))
            {
                AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                Console.ReadKey();

                continue;
            }

            switch(item)
            {
                case 0:
                    AaruLogging.WriteLine(Localization.Returning_to_Hewlett_Packard_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Physical_address_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out physical))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        physical = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Relative_address_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        relative = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(physical
                                          ? Localization.Physical_Block_Address_Q
                                          : Localization.Logical_Block_Address_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Transfer_sectors_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sectorCount))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sectorCount = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(sectorCount
                                          ? Localization.How_many_sectors_to_transfer_Q
                                          : Localization.How_many_bytes_to_transfer_Q);

                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (ushort)(sectorCount ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(sectorCount)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_sector_Q);
                        strDev = Console.ReadLine();

                        if(!ushort.TryParse(strDev, out bps))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            bps = 512;
                            Console.ReadKey();
                        }
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.HpReadLong(out byte[] buffer,
                                    out ReadOnlySpan<byte> senseBuffer,
                                    relative,
                                    address,
                                    length,
                                    bps,
                                    physical,
                                    sectorCount,
                                    dev.Timeout,
                                    out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_LONG_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_Hewlett_Packard_vendor_commands_menu);
        AaruLogging.Write(Localization.Choose);

        strDev = Console.ReadLine();

        if(!int.TryParse(strDev, out item))
        {
            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
            Console.ReadKey();
            Console.Clear();

            goto menu;
        }

        switch(item)
        {
            case 0:
                AaruLogging.WriteLine(Localization.Returning_to_Hewlett_Packard_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_decoded_sense);
                AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                goto start;
            case 5:
                goto parameters;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }
}