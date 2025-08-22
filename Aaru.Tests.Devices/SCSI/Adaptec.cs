// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Adaptec.cs
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
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.SCSI;

static class Adaptec
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_an_Adaptec_vendor_command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_READ_RESET_USAGE_COUNTER_command);
            AaruLogging.WriteLine(Localization.Send_READ_DATA_BUFFER_command);
            AaruLogging.WriteLine(Localization.Send_SET_ERROR_THRESHOLD_command);
            AaruLogging.WriteLine(Localization.Send_TRANSLATE_command);
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
                    ReadResetUsageCounter(devPath, dev);

                    continue;
                case 2:
                    ReadDataBuffer(devPath, dev);

                    continue;
                case 3:
                    SetErrorThreshold(devPath, dev);

                    continue;
                case 4:
                    Translate(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void ReadResetUsageCounter(string devPath, Device dev)
    {
        bool   drive1 = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_RESET_USAGE_COUNTER_command);
            AaruLogging.WriteLine(Localization.Drive_one_0, drive1);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Drive_1_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out drive1))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        drive1 = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.AdaptecReadUsageCounter(out byte[] buffer,
                                                 out ReadOnlySpan<byte> senseBuffer,
                                                 drive1,
                                                 dev.Timeout,
                                                 out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_RESET_USAGE_COUNTER_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_RESET_USAGE_COUNTER_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_RESET_USAGE_COUNTER_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_RESET_USAGE_COUNTER_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
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

    static void ReadDataBuffer(string devPath, Device dev)
    {
    start:
        Console.Clear();

        bool sense = dev.AdaptecReadBuffer(out byte[] buffer,
                                           out ReadOnlySpan<byte> senseBuffer,
                                           dev.Timeout,
                                           out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_DATA_BUFFER_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);
        AaruLogging.Write(Localization.Choose);

        string strDev = Console.ReadLine();

        if(!int.TryParse(strDev, out int item))
        {
            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
            Console.ReadKey();
            Console.Clear();

            goto menu;
        }

        switch(item)
        {
            case 0:
                AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DATA_BUFFER_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DATA_BUFFER_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DATA_BUFFER_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                goto start;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }

    static void SetErrorThreshold(string devPath, Device dev)
    {
        bool   drive1    = false;
        byte   threshold = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SET_ERROR_THRESHOLD_command);
            AaruLogging.WriteLine(Localization.Drive_one_0,       drive1);
            AaruLogging.WriteLine(Localization.Error_threshold_0, threshold);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Drive_1_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out drive1))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        drive1 = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Error_threshold);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out threshold))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        threshold = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.AdaptecSetErrorThreshold(threshold,
                                                  out ReadOnlySpan<byte> senseBuffer,
                                                  drive1,
                                                  dev.Timeout,
                                                  out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SET_ERROR_THRESHOLD_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.SET_ERROR_THRESHOLD_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SET_ERROR_THRESHOLD_sense);

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

    static void Translate(string devPath, Device dev)
    {
        bool   drive1 = false;
        uint   lba    = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_TRANSLATE_command);
            AaruLogging.WriteLine(Localization.Drive_one_0, drive1);
            AaruLogging.WriteLine(Localization.LBA_0,       lba);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Drive_1_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out drive1))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        drive1 = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.AdaptecTranslate(out byte[] buffer,
                                          out ReadOnlySpan<byte> senseBuffer,
                                          drive1,
                                          lba,
                                          dev.Timeout,
                                          out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_TRANSLATE_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Adaptec_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Adaptec_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.TRANSLATE_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.TRANSLATE_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.TRANSLATE_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
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