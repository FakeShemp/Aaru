// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Fujitsu.cs
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

static class Fujitsu
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_Fujitsu_vendor_command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_DISPLAY_command);
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
                    Display(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void Display(string devPath, Device dev)
    {
        var                 flash      = false;
        FujitsuDisplayModes mode       = FujitsuDisplayModes.Ready;
        var                 firstHalf  = "AARUTEST";
        var                 secondHalf = "TESTAARU";
        string              strDev;
        int                 item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_DISPLAY_command);
            AaruLogging.WriteLine(Localization.Descriptor_0, flash);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Fujitsu_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Fujitsu_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write("Flash?: ");
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out flash))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        flash = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Display_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4,
                                          FujitsuDisplayModes.Cancel,
                                          FujitsuDisplayModes.Cart,
                                          FujitsuDisplayModes.Half,
                                          FujitsuDisplayModes.Idle,
                                          FujitsuDisplayModes.Ready);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out mode))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_display_mode_Press_any_key_to_continue);
                        mode = FujitsuDisplayModes.Ready;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.First_display_half_will_be_cut_to_7_bit_ASCII_8_chars);
                    firstHalf = Console.ReadLine();
                    AaruLogging.Write(Localization.Second_display_half_will_be_cut_to_7_bit_ASCII_8_chars);
                    secondHalf = Console.ReadLine();

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.FujitsuDisplay(out ReadOnlySpan<byte> senseBuffer,
                                        flash,
                                        mode,
                                        firstHalf,
                                        secondHalf,
                                        dev.Timeout,
                                        out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_DISPLAY_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.DISPLAY_decoded_sense);
        AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_Fujitsu_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Fujitsu_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.DISPLAY_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                goto start;
            case 3:
                goto parameters;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }
}