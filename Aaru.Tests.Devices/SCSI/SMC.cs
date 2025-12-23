// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SMC.cs
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

static class Smc
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_SCSI_Media_Changer_command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_READ_ATTRIBUTE_command);
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
                    ReadAttribute(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void ReadAttribute(string devPath, Device dev)
    {
        ushort              element        = 0;
        byte                elementType    = 0;
        byte                volume         = 0;
        byte                partition      = 0;
        ushort              firstAttribute = 0;
        var                 cache          = false;
        ScsiAttributeAction action         = ScsiAttributeAction.Values;
        string              strDev;
        int                 item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_ATTRIBUTE_command);
            AaruLogging.WriteLine(Localization.Action_0,          action);
            AaruLogging.WriteLine(Localization.Element_0,         element);
            AaruLogging.WriteLine(Localization.Element_type_0,    elementType);
            AaruLogging.WriteLine(Localization.Volume_0,          volume);
            AaruLogging.WriteLine(Localization.Partition_0,       partition);
            AaruLogging.WriteLine(Localization.First_attribute_0, firstAttribute);
            AaruLogging.WriteLine(Localization.Use_cache_0,       cache);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Media_Changer_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Media_Changer_commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Attribute_action);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4,
                                          ScsiAttributeAction.Values,
                                          ScsiAttributeAction.List,
                                          ScsiAttributeAction.VolumeList,
                                          ScsiAttributeAction.PartitionList,
                                          ScsiAttributeAction.ElementList,
                                          ScsiAttributeAction.Supported);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out action))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_valid_attribute_action_Press_any_key_to_continue);
                        action = ScsiAttributeAction.Values;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Element_Q);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out element))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        element = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Element_type_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out elementType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        elementType = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Volume_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out volume))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        volume = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Partition_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out partition))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        partition = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.First_attribute_Q);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out firstAttribute))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        firstAttribute = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Use_cache_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out cache))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        cache = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadAttribute(out byte[] buffer,
                                       out ReadOnlySpan<byte> senseBuffer,
                                       action,
                                       element,
                                       elementType,
                                       volume,
                                       partition,
                                       firstAttribute,
                                       cache,
                                       dev.Timeout,
                                       out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_ATTRIBUTE_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Media_Changer_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Media_Changer_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_ATTRIBUTE_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_ATTRIBUTE_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_ATTRIBUTE_decoded_sense);
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