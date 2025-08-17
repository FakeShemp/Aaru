// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MultiMediaCard.cs
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
using Aaru.Devices;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.SecureDigital;

static class MultiMediaCard
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_MultiMediaCard_command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_READ_MULTIPLE_BLOCK_command);
            AaruLogging.WriteLine(Localization._2_Send_READ_SINGLE_BLOCK_command);
            AaruLogging.WriteLine(Localization._3_Send_SEND_CID_command);
            AaruLogging.WriteLine(Localization._4_Send_SEND_CSD_command);
            AaruLogging.WriteLine(Localization._5_Send_SEND_EXT_CSD_command);
            AaruLogging.WriteLine(Localization._6_Send_SEND_OP_COND_command);
            AaruLogging.WriteLine(Localization._7_Send_SEND_STATUS_command);
            AaruLogging.WriteLine(Localization._8_Send_SET_BLOCKLEN_command);
            AaruLogging.WriteLine(Localization.Return_to_SecureDigital_MultiMediaCard_commands_menu);
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
                    AaruLogging.WriteLine(Localization.Returning_to_SecureDigital_MultiMediaCard_commands_menu);

                    return;
                case 1:
                    Read(devPath, dev, true);

                    continue;
                case 2:
                    Read(devPath, dev, false);

                    continue;
                case 3:
                    SendCid(devPath, dev);

                    continue;
                case 4:
                    SendCsd(devPath, dev);

                    continue;
                case 5:
                    SendExtendedCsd(devPath, dev);

                    continue;
                case 6:
                    SendOpCond(devPath, dev);

                    continue;
                case 7:
                    Status(devPath, dev);

                    continue;
                case 8:
                    SetBlockLength(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void Read(string devPath, Device dev, bool multiple)
    {
        uint   address   = 0;
        uint   blockSize = 512;
        ushort count     = 1;
        bool   byteAddr  = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);

            AaruLogging.WriteLine(multiple
                                      ? Localization.Parameters_for_READ_MULTIPLE_BLOCK_command
                                      : Localization.Parameters_for_READ_SINGLE_BLOCK_command);

            AaruLogging.WriteLine(byteAddr ? Localization.Read_from_byte_0 : Localization.Read_from_block_0, address);
            AaruLogging.WriteLine(Localization.Expected_block_size_0_bytes,                                  blockSize);

            if(multiple) AaruLogging.WriteLine(Localization.Will_read_0_blocks, count);

            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Use_byte_addressing_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out byteAddr))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        byteAddr = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(byteAddr ? Localization.Read_from_byte_Q : Localization.Read_from_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(multiple)
                    {
                        AaruLogging.Write(Localization.How_many_blocks_to_read_Q);
                        strDev = Console.ReadLine();

                        if(!ushort.TryParse(strDev, out count))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            count = 1;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Read(out byte[] buffer,
                              out uint[] response,
                              address,
                              blockSize,
                              multiple ? count : (ushort)1,
                              byteAddr,
                              dev.Timeout,
                              out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);

        AaruLogging.WriteLine(multiple
                                  ? Localization.Sending_READ_MULTIPLE_BLOCK_to_the_device
                                  : Localization.Sending_READ_SINGLE_BLOCK_to_the_device);

        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_response_buffer);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization._4_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(multiple
                                          ? Localization.READ_MULTIPLE_BLOCK_buffer
                                          : Localization.READ_SINGLE_BLOCK_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(multiple
                                          ? Localization.READ_MULTIPLE_BLOCK_response
                                          : Localization.READ_SINGLE_BLOCK_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                goto start;
            case 4:
                goto parameters;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }

    static void SendOpCond(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ReadOcr(out byte[] buffer, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEND_OP_COND_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_response_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_OP_COND_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_OP_COND_decoded_buffer);

                if(buffer != null) AaruLogging.WriteLine("{0}", Decoders.MMC.Decoders.PrettifyOCR(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_OP_COND_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

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

    static void Status(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ReadSdStatus(out byte[] buffer, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEND_STATUS_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_response_buffer);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_STATUS_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_STATUS_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                goto start;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }

    static void SendCid(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ReadCid(out byte[] buffer, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEND_CID_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_response_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CID_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CID_decoded_buffer);

                if(buffer != null) AaruLogging.WriteLine("{0}", Decoders.MMC.Decoders.PrettifyCID(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CID_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

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

    static void SendCsd(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ReadCsd(out byte[] buffer, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEND_CSD_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_response_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CSD_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CSD_decoded_buffer);

                if(buffer != null) AaruLogging.WriteLine("{0}", Decoders.MMC.Decoders.PrettifyCSD(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_CSD_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

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

    static void SendExtendedCsd(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ReadExtendedCsd(out byte[] buffer, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEND_EXT_CSD_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_response_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_EXT_CSD_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_EXT_CSD_decoded_buffer);

                if(buffer != null) AaruLogging.WriteLine("{0}", Decoders.MMC.Decoders.PrettifyExtendedCSD(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEND_EXT_CSD_response);

                if(response != null)
                {
                    foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

                    AaruLogging.WriteLine();
                }

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

    static void SetBlockLength(string devPath, Device dev)
    {
        uint   blockSize = 512;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SET_BLOCKLEN_command);
            AaruLogging.WriteLine(Localization.Set_block_length_to_0_bytes, blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Set_block_length_to_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.SetBlockLength(blockSize, out uint[] response, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SET_BLOCKLEN_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms,       duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,              sense);
        AaruLogging.WriteLine(Localization.Response_has_0_elements, response?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.SET_BLOCKLEN_response);

        if(response != null)
        {
            foreach(uint res in response) AaruLogging.Write("0x{0:x8} ", res);

            AaruLogging.WriteLine();
        }

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Send_command_again);
        AaruLogging.WriteLine(Localization._2_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_MultiMediaCard_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_MultiMediaCard_commands_menu);

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