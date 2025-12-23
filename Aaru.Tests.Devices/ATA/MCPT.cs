// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MCPT.cs
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
using Aaru.Decoders.ATA;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Tests.Devices.ATA;

static class Mcpt
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_Media_Card_Pass_Through_command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_CHECK_MEDIA_CARD_TYPE_command);
            AaruLogging.WriteLine(Localization.Return_to_ATA_commands_menu);
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
                    AaruLogging.WriteLine(Localization.Returning_to_ATA_commands_menu);

                    return;
                case 1:
                    CheckMediaCardType(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void CheckMediaCardType(string devPath, Device dev)
    {
        byte   feature = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_CHECK_MEDIA_CARD_TYPE_command);
            AaruLogging.WriteLine(Localization.Feature_0, feature);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Media_Card_Pass_Through_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Media_Card_Pass_Through_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Feature);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out feature))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        feature = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.CheckMediaCardType(feature,
                                            out AtaErrorRegistersChs errorRegisters,
                                            dev.Timeout,
                                            out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_CHECK_MEDIA_CARD_TYPE_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);
        AaruLogging.WriteLine(Localization.CHECK_MEDIA_CARD_TYPE_status_registers);
        AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Send_command_again);
        AaruLogging.WriteLine(Localization._2_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_Media_Card_Pass_Through_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Media_Card_Pass_Through_commands_menu);

                return;
            case 1:
                goto start;
            case 2:
                goto parameters;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }
}