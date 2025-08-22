// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Pioneer.cs
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

static class Pioneer
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_Pioneer_vendor_command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_READ_CD_DA_command);
            AaruLogging.WriteLine(Localization._2_Send_READ_CD_DA_MSF_command);
            AaruLogging.WriteLine(Localization._3_Send_READ_CD_XA_command);
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
                    ReadCdDa(devPath, dev);

                    continue;
                case 2:
                    ReadCdDaMsf(devPath, dev);

                    continue;
                case 3:
                    ReadCdXa(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void ReadCdDa(string devPath, Device dev)
    {
        uint              address   = 0;
        uint              length    = 1;
        PioneerSubchannel subchan   = PioneerSubchannel.None;
        uint              blockSize = 2352;
        string            strDev;
        int               item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CD_DA_command);
            AaruLogging.WriteLine(Localization.LBA_0,                   address);
            AaruLogging.WriteLine(Localization.Will_transfer_0_sectors, length);
            AaruLogging.WriteLine(Localization.Subchannel_mode_0,       subchan);
            AaruLogging.WriteLine(Localization._0_bytes_per_sector,     blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Logical_Block_Address_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_sectors_to_transfer_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Subchannel_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          PioneerSubchannel.None,
                                          PioneerSubchannel.Q16,
                                          PioneerSubchannel.All,
                                          PioneerSubchannel.Only);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out subchan))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_subchannel_mode_Press_any_key_to_continue);
                        subchan = PioneerSubchannel.None;
                        Console.ReadKey();

                        continue;
                    }

                    blockSize = subchan switch
                                {
                                    PioneerSubchannel.Q16  => 2368,
                                    PioneerSubchannel.All  => 2448,
                                    PioneerSubchannel.Only => 96,
                                    _                      => 2352
                                };

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.PioneerReadCdDa(out byte[] buffer,
                                         out ReadOnlySpan<byte> senseBuffer,
                                         address,
                                         blockSize,
                                         length,
                                         subchan,
                                         dev.Timeout,
                                         out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CD_DA_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_decoded_sense);
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

    static void ReadCdDaMsf(string devPath, Device dev)
    {
        byte              startFrame  = 0;
        byte              startSecond = 2;
        byte              startMinute = 0;
        byte              endFrame    = 0;
        const byte        endSecond   = 0;
        byte              endMinute   = 0;
        PioneerSubchannel subchan     = PioneerSubchannel.None;
        uint              blockSize   = 2352;
        string            strDev;
        int               item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CD_DA_MSF_command);
            AaruLogging.WriteLine(Localization.Start_0_1_2,         startMinute, startSecond, startFrame);
            AaruLogging.WriteLine(Localization.End_0_1_2,           endMinute,   endSecond,   endFrame);
            AaruLogging.WriteLine(Localization.Subchannel_mode_0,   subchan);
            AaruLogging.WriteLine(Localization._0_bytes_per_sector, blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Start_minute_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out startMinute))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        startMinute = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Start_second_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out startSecond))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        startSecond = 2;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Start_frame_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out startFrame))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        startFrame = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.End_minute_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out endMinute))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        endMinute = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.End_second_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out endMinute))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        endMinute = 2;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.End_frame_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out endFrame))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        endFrame = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Subchannel_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          PioneerSubchannel.None,
                                          PioneerSubchannel.Q16,
                                          PioneerSubchannel.All,
                                          PioneerSubchannel.Only);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out subchan))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_subchannel_mode_Press_any_key_to_continue);
                        subchan = PioneerSubchannel.None;
                        Console.ReadKey();

                        continue;
                    }

                    blockSize = subchan switch
                                {
                                    PioneerSubchannel.Q16  => 2368,
                                    PioneerSubchannel.All  => 2448,
                                    PioneerSubchannel.Only => 96,
                                    _                      => 2352
                                };

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        uint startMsf = (uint)((startMinute << 16) + (startSecond << 8) + startFrame);
        uint endMsf   = (uint)((startMinute << 16) + (startSecond << 8) + startFrame);
        Console.Clear();

        bool sense = dev.PioneerReadCdDaMsf(out byte[] buffer,
                                            out ReadOnlySpan<byte> senseBuffer,
                                            startMsf,
                                            endMsf,
                                            blockSize,
                                            subchan,
                                            dev.Timeout,
                                            out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CD_DA_MSF_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_MSF_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_MSF_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_DA_MSF_decoded_sense);
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

    static void ReadCdXa(string devPath, Device dev)
    {
        uint   address     = 0;
        uint   length      = 1;
        bool   errorFlags  = false;
        bool   wholeSector = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CD_XA_command);
            AaruLogging.WriteLine(Localization.LBA_0,                   address);
            AaruLogging.WriteLine(Localization.Will_transfer_0_sectors, length);
            AaruLogging.WriteLine(Localization.Include_error_flags_0,   errorFlags);
            AaruLogging.WriteLine(Localization.Whole_sector_0,          wholeSector);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Logical_Block_Address_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_sectors_to_transfer_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = 1;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Include_error_flags_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out errorFlags))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        errorFlags = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Read_whole_sector_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out wholeSector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        wholeSector = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.PioneerReadCdXa(out byte[] buffer,
                                         out ReadOnlySpan<byte> senseBuffer,
                                         address,
                                         length,
                                         errorFlags,
                                         wholeSector,
                                         dev.Timeout,
                                         out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CD_XA_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_Pioneer_vendor_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_Pioneer_vendor_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_XA_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_XA_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_XA_decoded_sense);
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