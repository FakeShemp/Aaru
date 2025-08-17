// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SBC.cs
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

static class Sbc
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_SCSI_Block_Command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_READ_6_command);
            AaruLogging.WriteLine(Localization._2_Send_READ_10_command);
            AaruLogging.WriteLine(Localization._3_Send_READ_12_command);
            AaruLogging.WriteLine(Localization._4_Send_READ_16_command);
            AaruLogging.WriteLine(Localization._5_Send_READ_LONG_10_command);
            AaruLogging.WriteLine(Localization._6_Send_READ_LONG_16_command);
            AaruLogging.WriteLine(Localization._7_Send_SEEK_6_command);
            AaruLogging.WriteLine(Localization._8_Send_SEEK_10_command);
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
                    Read6(devPath, dev);

                    continue;
                case 2:
                    Read10(devPath, dev);

                    continue;
                case 3:
                    Read12(devPath, dev);

                    continue;
                case 4:
                    Read16(devPath, dev);

                    continue;
                case 5:
                    ReadLong10(devPath, dev);

                    continue;
                case 6:
                    ReadLong16(devPath, dev);

                    continue;
                case 7:
                    Seek6(devPath, dev);

                    continue;
                case 8:
                    Seek10(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void Read6(string devPath, Device dev)
    {
        uint   lba       = 0;
        uint   blockSize = 512;
        byte   count     = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_6_command);
            AaruLogging.WriteLine(Localization.LBA_0,                       lba);
            AaruLogging.WriteLine(Localization._0_blocks_to_read,           count == 0 ? 256 : count);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(lba > 0x1FFFFF)
                    {
                        AaruLogging.WriteLine(Localization.Max_LBA_is_0_setting_to_0, 0x1FFFFF);
                        lba = 0x1FFFFF;
                    }

                    AaruLogging.Write(Localization.Blocks_to_read_zero_for_256_blocks);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
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

        bool sense = dev.Read6(out byte[] buffer,
                               out byte[] senseBuffer,
                               lba,
                               blockSize,
                               count,
                               dev.Timeout,
                               out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_6_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_6_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_6_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_6_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void Read10(string devPath, Device dev)
    {
        uint       lba         = 0;
        uint       blockSize   = 512;
        byte       count       = 1;
        byte       rdprotect   = 0;
        bool       dpo         = false;
        bool       fua         = false;
        bool       fuaNv       = false;
        const byte groupNumber = 0;
        bool       relative    = false;
        string     strDev;
        int        item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_10_command);
            AaruLogging.WriteLine(Localization.Address_relative_to_current_position_0, relative);
            AaruLogging.WriteLine(relative ? Localization.Address_0 : Localization.LBA_0, lba);
            AaruLogging.WriteLine(Localization._0_blocks_to_read, count == 0 ? 256 : count);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);
            AaruLogging.WriteLine(Localization.How_to_check_protection_information_0, rdprotect);
            AaruLogging.WriteLine(Localization.Give_lowest_cache_priority_0, dpo);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_medium_0, fua);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_0, fuaNv);
            AaruLogging.WriteLine(Localization.Group_number_0, groupNumber);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Address_relative_to_current_position_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(relative ? Localization.Address_Q : Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Blocks_to_read_zero_for_256_blocks);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_to_check_protection_information_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out rdprotect))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Give_lowest_cache_priority_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dpo))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_medium_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fua))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fuaNv))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Group_number_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Read10(out byte[] buffer,
                                out byte[] senseBuffer,
                                rdprotect,
                                dpo,
                                fua,
                                fuaNv,
                                relative,
                                lba,
                                blockSize,
                                groupNumber,
                                count,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_10_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_10_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_10_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_10_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void Read12(string devPath, Device dev)
    {
        uint       lba         = 0;
        uint       blockSize   = 512;
        byte       count       = 1;
        byte       rdprotect   = 0;
        bool       dpo         = false;
        bool       fua         = false;
        bool       fuaNv       = false;
        const byte groupNumber = 0;
        bool       relative    = false;
        bool       streaming   = false;
        string     strDev;
        int        item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_12_command);
            AaruLogging.WriteLine(Localization.Address_relative_to_current_position_0, relative);
            AaruLogging.WriteLine(relative ? Localization.Address_0 : Localization.LBA_0, lba);
            AaruLogging.WriteLine(Localization._0_blocks_to_read, count == 0 ? 256 : count);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);
            AaruLogging.WriteLine(Localization.How_to_check_protection_information_0, rdprotect);
            AaruLogging.WriteLine(Localization.Give_lowest_cache_priority_0, dpo);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_medium_0, fua);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_0, fuaNv);
            AaruLogging.WriteLine(Localization.Group_number_0, groupNumber);
            AaruLogging.WriteLine(Localization.Use_streaming_0, streaming);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Address_relative_to_current_position_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(relative ? Localization.Address_Q : Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Blocks_to_read_zero_for_256_blocks);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_to_check_protection_information_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out rdprotect))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Give_lowest_cache_priority_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dpo))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_medium_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fua))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fuaNv))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Group_number_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Use_streaming_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out streaming))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
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

        bool sense = dev.Read12(out byte[] buffer,
                                out byte[] senseBuffer,
                                rdprotect,
                                dpo,
                                fua,
                                fuaNv,
                                relative,
                                lba,
                                blockSize,
                                groupNumber,
                                count,
                                streaming,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_12_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_12_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_12_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_12_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void Read16(string devPath, Device dev)
    {
        ulong      lba         = 0;
        uint       blockSize   = 512;
        byte       count       = 1;
        byte       rdprotect   = 0;
        bool       dpo         = false;
        bool       fua         = false;
        bool       fuaNv       = false;
        const byte groupNumber = 0;
        bool       streaming   = false;
        string     strDev;
        int        item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_16_command);
            AaruLogging.WriteLine(Localization.LBA_0, lba);
            AaruLogging.WriteLine(Localization._0_blocks_to_read, count == 0 ? 256 : count);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);
            AaruLogging.WriteLine(Localization.How_to_check_protection_information_0, rdprotect);
            AaruLogging.WriteLine(Localization.Give_lowest_cache_priority_0, dpo);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_medium_0, fua);
            AaruLogging.WriteLine(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_0, fuaNv);
            AaruLogging.WriteLine(Localization.Group_number_0, groupNumber);
            AaruLogging.WriteLine(Localization.Use_streaming_0, streaming);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Blocks_to_read_zero_for_256_blocks);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_to_check_protection_information_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out rdprotect))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Give_lowest_cache_priority_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dpo))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_medium_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fua))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Force_bypassing_cache_and_reading_from_non_volatile_cache_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fuaNv))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Group_number_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Use_streaming_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out streaming))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
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

        bool sense = dev.Read16(out byte[] buffer,
                                out byte[] senseBuffer,
                                rdprotect,
                                dpo,
                                fua,
                                fuaNv,
                                lba,
                                blockSize,
                                groupNumber,
                                count,
                                streaming,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_16_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_16_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_16_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_16_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void ReadLong10(string devPath, Device dev)
    {
        uint   lba       = 0;
        ushort blockSize = 512;
        bool   correct   = false;
        bool   relative  = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_LONG_10_command);
            AaruLogging.WriteLine(Localization.Address_relative_to_current_position_0,    relative);
            AaruLogging.WriteLine(relative ? Localization.Address_0 : Localization.LBA_0, lba);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block,               blockSize);
            AaruLogging.WriteLine(Localization.Try_to_error_correct_block_0,              correct);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Address_relative_to_current_position_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(relative ? Localization.Address_Q : Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Try_to_error_correct_block_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out correct))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
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

        bool sense = dev.ReadLong10(out byte[] buffer,
                                    out byte[] senseBuffer,
                                    correct,
                                    relative,
                                    lba,
                                    blockSize,
                                    dev.Timeout,
                                    out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_LONG_10_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_10_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_10_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_10_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void ReadLong16(string devPath, Device dev)
    {
        ulong  lba       = 0;
        uint   blockSize = 512;
        bool   correct   = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_LONG_16_command);
            AaruLogging.WriteLine(Localization.LBA_0,                        lba);
            AaruLogging.WriteLine(Localization._0_bytes_expected_per_block,  blockSize);
            AaruLogging.WriteLine(Localization.Try_to_error_correct_block_0, correct);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 512;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Try_to_error_correct_block_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out correct))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
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

        bool sense = dev.ReadLong16(out byte[] buffer,
                                    out byte[] senseBuffer,
                                    correct,
                                    lba,
                                    blockSize,
                                    dev.Timeout,
                                    out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_LONG_16_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization._5_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_16_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_16_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_LONG_16_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
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

    static void Seek6(string devPath, Device dev)
    {
        uint   lba = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SEEK_6_command);
            AaruLogging.WriteLine(Localization.Descriptor_0, lba);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.LBA_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out lba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        lba = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(lba > 0x1FFFFF)
                    {
                        AaruLogging.WriteLine(Localization.Max_LBA_is_0_setting_to_0, 0x1FFFFF);
                        lba = 0x1FFFFF;
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.Seek6(out byte[] senseBuffer, lba, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEEK_6_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine(Localization.SEEK_6_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEEK_6_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

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

    static void Seek10(string devPath, Device dev)
    {
        uint   lba = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SEEK_10_command);
            AaruLogging.WriteLine(Localization.Descriptor_0, lba);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Descriptor_Q);
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
        bool sense = dev.Seek10(out byte[] senseBuffer, lba, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEEK_10_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes,
                              senseBuffer?.Length.ToString() ?? Localization._null);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine(Localization.SEEK_6_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Block_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Block_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEEK_10_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer, 64);

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