// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SSC.cs
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
using Aaru.Decoders.SCSI.SSC;
using Aaru.Devices;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.SCSI;

static class Ssc
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_SCSI_Streaming_Command_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_LOAD_UNLOAD_command);
            AaruLogging.WriteLine(Localization._2_Send_LOCATE_10_command);
            AaruLogging.WriteLine(Localization._3_Send_LOCATE_16_command);
            AaruLogging.WriteLine(Localization._4_Send_READ_6_command);
            AaruLogging.WriteLine(Localization._5_Send_READ_16_command);
            AaruLogging.WriteLine(Localization._6_Send_READ_BLOCK_LIMITS_command);
            AaruLogging.WriteLine(Localization._7_Send_READ_POSITION_command);
            AaruLogging.WriteLine(Localization._8_Send_READ_REVERSE_6_command);
            AaruLogging.WriteLine(Localization._9_Send_READ_REVERSE_16_command);
            AaruLogging.WriteLine(Localization._10_Send_RECOVER_BUFFERED_DATA_command);
            AaruLogging.WriteLine(Localization._11_Send_REPORT_DENSITY_SUPPORT_command);
            AaruLogging.WriteLine(Localization._12_Send_REWIND_command);
            AaruLogging.WriteLine(Localization._13_Send_SPACE_command);
            AaruLogging.WriteLine(Localization._14_Send_TRACK_SELECT_command);
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
                    LoadUnload(devPath, dev);

                    continue;
                case 2:
                    Locate10(devPath, dev);

                    continue;
                case 3:
                    Locate16(devPath, dev);

                    continue;
                case 4:
                    Read6(devPath, dev);

                    continue;
                case 5:
                    Read16(devPath, dev);

                    continue;
                case 6:
                    ReadBlockLimits(devPath, dev);

                    continue;
                case 7:
                    ReadPosition(devPath, dev);

                    continue;
                case 8:
                    ReadReverse6(devPath, dev);

                    continue;
                case 9:
                    ReadReverse16(devPath, dev);

                    continue;
                case 10:
                    RecoverBufferedData(devPath, dev);

                    continue;
                case 11:
                    ReportDensitySupport(devPath, dev);

                    continue;
                case 12:
                    Rewind(devPath, dev);

                    continue;
                case 13:
                    Space(devPath, dev);

                    continue;
                case 14:
                    TrackSelect(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void LoadUnload(string devPath, Device dev)
    {
        bool   load      = true;
        bool   immediate = false;
        bool   retense   = false;
        bool   eot       = false;
        bool   hold      = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_LOAD_UNLOAD_command);
            AaruLogging.WriteLine(Localization.Load_0,        load);
            AaruLogging.WriteLine(Localization.Immediate_0,   immediate);
            AaruLogging.WriteLine(Localization.Retense_0,     retense);
            AaruLogging.WriteLine(Localization.End_of_tape_0, eot);
            AaruLogging.WriteLine(Localization.Hold_0,        hold);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Load_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out load))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        load = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Immediate_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out immediate))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        immediate = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retense_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out retense))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        retense = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.End_of_tape_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out eot))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        eot = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Hold_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out hold))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        hold = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.LoadUnload(out ReadOnlySpan<byte> senseBuffer,
                                    immediate,
                                    load,
                                    retense,
                                    eot,
                                    hold,
                                    dev.Timeout,
                                    out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_LOAD_UNLOAD_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.LOAD_UNLOAD_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.LOAD_UNLOAD_sense);

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

    static void Locate10(string devPath, Device dev)
    {
        bool   blockType       = true;
        bool   immediate       = false;
        bool   changePartition = false;
        byte   partition       = 0;
        uint   objectId        = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_LOCATE_10_command);
            AaruLogging.WriteLine(Localization.Locate_block_0,                              blockType);
            AaruLogging.WriteLine(Localization.Immediate_0,                                 immediate);
            AaruLogging.WriteLine(Localization.Change_partition_0,                          changePartition);
            AaruLogging.WriteLine(Localization.Partition_0,                                 partition);
            AaruLogging.WriteLine(blockType ? Localization.Block_0 : Localization.Object_0, objectId);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Load_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out blockType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        blockType = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Immediate_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out immediate))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        immediate = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Change_partition_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out changePartition))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        changePartition = false;
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

                    AaruLogging.Write(blockType ? Localization.Block_Q : Localization.Object_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out objectId))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        objectId = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Locate(out ReadOnlySpan<byte> senseBuffer,
                                immediate,
                                blockType,
                                changePartition,
                                partition,
                                objectId,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_LOCATE_10_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.LOCATE_10_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.LOCATE_10_sense);

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

    static void Locate16(string devPath, Device dev)
    {
        SscLogicalIdTypes destType        = SscLogicalIdTypes.FileId;
        bool              immediate       = false;
        bool              changePartition = false;
        bool              bam             = false;
        byte              partition       = 0;
        ulong             objectId        = 1;
        string            strDev;
        int               item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_LOCATE_16_command);
            AaruLogging.WriteLine(Localization.Object_type_0,         destType);
            AaruLogging.WriteLine(Localization.Immediate_0,           immediate);
            AaruLogging.WriteLine(Localization.Explicit_identifier_0, bam);
            AaruLogging.WriteLine(Localization.Change_partition_0,    changePartition);
            AaruLogging.WriteLine(Localization.Partition_0,           partition);
            AaruLogging.WriteLine(Localization.Object_identifier_0,   objectId);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Object_type);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          SscLogicalIdTypes.FileId,
                                          SscLogicalIdTypes.ObjectId,
                                          SscLogicalIdTypes.Reserved,
                                          SscLogicalIdTypes.SetId);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out destType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_object_type_Press_any_key_to_continue);
                        destType = SscLogicalIdTypes.FileId;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Immediate_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out immediate))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        immediate = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Explicit_identifier_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out bam))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        bam = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Change_partition_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out changePartition))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        changePartition = false;
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

                    AaruLogging.Write(Localization.Identifier);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out objectId))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        objectId = 1;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Locate16(out ReadOnlySpan<byte> senseBuffer,
                                  immediate,
                                  changePartition,
                                  destType,
                                  bam,
                                  partition,
                                  objectId,
                                  dev.Timeout,
                                  out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_LOCATE_16_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.LOCATE_16_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.LOCATE_16_sense);

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

    static void Read6(string devPath, Device dev)
    {
        bool   sili      = false;
        bool   fixedLen  = true;
        uint   blockSize = 512;
        uint   length    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_6_command);
            AaruLogging.WriteLine(Localization.Fixed_block_size_0, fixedLen);
            AaruLogging.WriteLine(fixedLen ? Localization.Will_read_0_blocks : Localization.Will_read_0_bytes, length);

            if(fixedLen) AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);

            AaruLogging.WriteLine(Localization.Suppress_length_indicator_0, sili);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Fixed_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fixedLen))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        fixedLen = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(fixedLen
                                          ? Localization.How_many_blocks_to_read_Q
                                          : Localization.How_many_bytes_to_read_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (uint)(fixedLen ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(length > 0xFFFFFF)
                    {
                        AaruLogging.WriteLine(fixedLen
                                                  ? Localization.Max_number_of_blocks_is_0_setting_to_0
                                                  : Localization.Max_number_of_bytes_is_0_setting_to_0,
                                              0xFFFFFF);

                        length = 0xFFFFFF;
                    }

                    if(fixedLen)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                        strDev = Console.ReadLine();

                        if(!uint.TryParse(strDev, out blockSize))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            blockSize = 512;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.Suppress_length_indicator_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sili))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sili = false;
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
                               out ReadOnlySpan<byte> senseBuffer,
                               sili,
                               fixedLen,
                               length,
                               blockSize,
                               dev.Timeout,
                               out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_6_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

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

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_6_decoded_sense);
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

    static void Read16(string devPath, Device dev)
    {
        bool   sili       = false;
        bool   fixedLen   = true;
        uint   objectSize = 512;
        uint   length     = 1;
        byte   partition  = 0;
        ulong  objectId   = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_16_command);
            AaruLogging.WriteLine(Localization.Fixed_block_size_0, fixedLen);
            AaruLogging.WriteLine(fixedLen ? Localization.Will_read_0_objects : Localization.Will_read_0_bytes, length);

            if(fixedLen) AaruLogging.WriteLine(Localization._0_bytes_expected_per_object, objectSize);

            AaruLogging.WriteLine(Localization.Suppress_length_indicator_0,    sili);
            AaruLogging.WriteLine(Localization.Read_object_0_from_partition_1, objectId, partition);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Fixed_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fixedLen))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        fixedLen = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(fixedLen
                                          ? Localization.How_many_objects_to_read_Q
                                          : Localization.How_many_bytes_to_read_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (uint)(fixedLen ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(length > 0xFFFFFF)
                    {
                        AaruLogging.WriteLine(fixedLen
                                                  ? Localization.Max_number_of_blocks_is_0_setting_to_0
                                                  : Localization.Max_number_of_bytes_is_0_setting_to_0,
                                              0xFFFFFF);

                        length = 0xFFFFFF;
                    }

                    if(fixedLen)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_object_Q);
                        strDev = Console.ReadLine();

                        if(!uint.TryParse(strDev, out objectSize))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            objectSize = 512;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.Suppress_length_indicator_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sili))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sili = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Object_identifier_Q);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out objectId))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        objectId = 0;
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
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Read16(out byte[] buffer,
                                out ReadOnlySpan<byte> senseBuffer,
                                sili,
                                fixedLen,
                                partition,
                                objectId,
                                length,
                                objectSize,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_16_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

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

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_16_decoded_sense);
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

    static void ReadBlockLimits(string devPath, Device dev)
    {
    start:
        Console.Clear();

        bool sense = dev.ReadBlockLimits(out byte[] buffer,
                                         out ReadOnlySpan<byte> senseBuffer,
                                         dev.Timeout,
                                         out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_BLOCK_LIMITS_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._5_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_BLOCK_LIMITS_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_BLOCK_LIMITS_decoded_response);

                if(buffer != null) AaruLogging.WriteLine("{0}", BlockLimits.Prettify(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_BLOCK_LIMITS_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_BLOCK_LIMITS_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 5:
                goto start;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }

    static void ReadPosition(string devPath, Device dev)
    {
        SscPositionForms responseForm = SscPositionForms.Short;
        string           strDev;
        int              item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_LOCATE_16_command);
            AaruLogging.WriteLine(Localization.Response_form_0, responseForm);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Response_form);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4_5_6_7_8,
                                          SscPositionForms.Short,
                                          SscPositionForms.VendorShort,
                                          SscPositionForms.OldLong,
                                          SscPositionForms.OldLongVendor,
                                          SscPositionForms.OldTclp,
                                          SscPositionForms.OldTclpVendor,
                                          SscPositionForms.Long,
                                          SscPositionForms.OldLongTclpVendor,
                                          SscPositionForms.Extended);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out responseForm))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_response_form_Press_any_key_to_continue);
                        responseForm = SscPositionForms.Short;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadPosition(out _,
                                      out ReadOnlySpan<byte> senseBuffer,
                                      responseForm,
                                      dev.Timeout,
                                      out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_POSITION_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.READ_POSITION_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_POSITION_sense);

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

    static void ReadReverse6(string devPath, Device dev)
    {
        bool   byteOrder = false;
        bool   sili      = false;
        bool   fixedLen  = true;
        uint   blockSize = 512;
        uint   length    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_REVERSE_6_command);
            AaruLogging.WriteLine(Localization.Fixed_block_size_0, fixedLen);
            AaruLogging.WriteLine(fixedLen ? Localization.Will_read_0_blocks : Localization.Will_read_0_bytes, length);

            if(fixedLen) AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);

            AaruLogging.WriteLine(Localization.Suppress_length_indicator_0,    sili);
            AaruLogging.WriteLine(Localization.Drive_should_unreverse_bytes_0, byteOrder);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Fixed_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fixedLen))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        fixedLen = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(fixedLen
                                          ? Localization.How_many_blocks_to_read_Q
                                          : Localization.How_many_bytes_to_read_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (uint)(fixedLen ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(length > 0xFFFFFF)
                    {
                        AaruLogging.WriteLine(fixedLen
                                                  ? Localization.Max_number_of_blocks_is_0_setting_to_0
                                                  : Localization.Max_number_of_bytes_is_0_setting_to_0,
                                              0xFFFFFF);

                        length = 0xFFFFFF;
                    }

                    if(fixedLen)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                        strDev = Console.ReadLine();

                        if(!uint.TryParse(strDev, out blockSize))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            blockSize = 512;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.Suppress_length_indicator_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sili))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sili = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Drive_should_unreverse_bytes_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out byteOrder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        byteOrder = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadReverse6(out byte[] buffer,
                                      out ReadOnlySpan<byte> senseBuffer,
                                      byteOrder,
                                      sili,
                                      fixedLen,
                                      length,
                                      blockSize,
                                      dev.Timeout,
                                      out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_REVERSE_6_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_6_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_6_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_6_decoded_sense);
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

    static void ReadReverse16(string devPath, Device dev)
    {
        bool   byteOrder  = false;
        bool   sili       = false;
        bool   fixedLen   = true;
        uint   objectSize = 512;
        uint   length     = 1;
        byte   partition  = 0;
        ulong  objectId   = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_REVERSE_16_command);
            AaruLogging.WriteLine(Localization.Fixed_block_size_0, fixedLen);
            AaruLogging.WriteLine(fixedLen ? Localization.Will_read_0_objects : Localization.Will_read_0_bytes, length);

            if(fixedLen) AaruLogging.WriteLine(Localization._0_bytes_expected_per_object, objectSize);

            AaruLogging.WriteLine(Localization.Suppress_length_indicator_0,    sili);
            AaruLogging.WriteLine(Localization.Read_object_0_from_partition_1, objectId, partition);
            AaruLogging.WriteLine(Localization.Drive_should_unreverse_bytes_0, byteOrder);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Fixed_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fixedLen))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        fixedLen = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(fixedLen
                                          ? Localization.How_many_objects_to_read_Q
                                          : Localization.How_many_bytes_to_read_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (uint)(fixedLen ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(length > 0xFFFFFF)
                    {
                        AaruLogging.WriteLine(fixedLen
                                                  ? Localization.Max_number_of_blocks_is_0_setting_to_0
                                                  : Localization.Max_number_of_bytes_is_0_setting_to_0,
                                              0xFFFFFF);

                        length = 0xFFFFFF;
                    }

                    if(fixedLen)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_object_Q);
                        strDev = Console.ReadLine();

                        if(!uint.TryParse(strDev, out objectSize))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            objectSize = 512;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.Suppress_length_indicator_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sili))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sili = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Object_identifier_Q);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out objectId))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        objectId = 0;
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

                    AaruLogging.Write(Localization.Drive_should_unreverse_bytes_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out byteOrder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        byteOrder = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadReverse16(out byte[] buffer,
                                       out ReadOnlySpan<byte> senseBuffer,
                                       byteOrder,
                                       sili,
                                       fixedLen,
                                       partition,
                                       objectId,
                                       length,
                                       objectSize,
                                       dev.Timeout,
                                       out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_REVERSE_16_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_16_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_16_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_REVERSE_16_decoded_sense);
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

    static void RecoverBufferedData(string devPath, Device dev)
    {
        bool   sili      = false;
        bool   fixedLen  = true;
        uint   blockSize = 512;
        uint   length    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_RECOVER_BUFFERED_DATA_command);
            AaruLogging.WriteLine(Localization.Fixed_block_size_0, fixedLen);
            AaruLogging.WriteLine(fixedLen ? Localization.Will_read_0_blocks : Localization.Will_read_0_bytes, length);

            if(fixedLen) AaruLogging.WriteLine(Localization._0_bytes_expected_per_block, blockSize);

            AaruLogging.WriteLine(Localization.Suppress_length_indicator_0, sili);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Fixed_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out fixedLen))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        fixedLen = true;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(fixedLen
                                          ? Localization.How_many_blocks_to_read_Q
                                          : Localization.How_many_bytes_to_read_Q);

                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out length))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        length = (uint)(fixedLen ? 1 : 512);
                        Console.ReadKey();

                        continue;
                    }

                    if(length > 0xFFFFFF)
                    {
                        AaruLogging.WriteLine(fixedLen
                                                  ? Localization.Max_number_of_blocks_is_0_setting_to_0
                                                  : Localization.Max_number_of_bytes_is_0_setting_to_0,
                                              0xFFFFFF);

                        length = 0xFFFFFF;
                    }

                    if(fixedLen)
                    {
                        AaruLogging.Write(Localization.How_many_bytes_to_expect_per_block_Q);
                        strDev = Console.ReadLine();

                        if(!uint.TryParse(strDev, out blockSize))
                        {
                            AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                            blockSize = 512;
                            Console.ReadKey();

                            continue;
                        }
                    }

                    AaruLogging.Write(Localization.Suppress_length_indicator_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sili))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sili = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.RecoverBufferedData(out byte[] buffer,
                                             out ReadOnlySpan<byte> senseBuffer,
                                             sili,
                                             fixedLen,
                                             length,
                                             blockSize,
                                             dev.Timeout,
                                             out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_RECOVER_BUFFERED_DATA_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.RECOVER_BUFFERED_DATA_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.RECOVER_BUFFERED_DATA_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.RECOVER_BUFFERED_DATA_decoded_sense);
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

    static void ReportDensitySupport(string devPath, Device dev)
    {
        bool   medium  = false;
        bool   current = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_REPORT_DENSITY_SUPPORT_command);
            AaruLogging.WriteLine(Localization.Report_about_medium_types_0,   medium);
            AaruLogging.WriteLine(Localization.Report_about_current_medium_0, current);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Report_about_medium_types_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out medium))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        medium = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Report_about_current_medium_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out current))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        current = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReportDensitySupport(out byte[] buffer,
                                              out ReadOnlySpan<byte> senseBuffer,
                                              medium,
                                              current,
                                              dev.Timeout,
                                              out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_REPORT_DENSITY_SUPPORT_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._4_Decode_sense_buffer);
        AaruLogging.WriteLine(Localization._5_Send_command_again);
        AaruLogging.WriteLine(Localization._6_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REPORT_DENSITY_SUPPORT_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REPORT_DENSITY_SUPPORT_decoded_buffer);

                AaruLogging.Write("{0}",
                                  medium
                                      ? DensitySupport.PrettifyMediumType(buffer)
                                      : DensitySupport.PrettifyDensity(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REPORT_DENSITY_SUPPORT_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REPORT_DENSITY_SUPPORT_decoded_sense);
                AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 5:
                goto start;
            case 6:
                goto parameters;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }

    static void Rewind(string devPath, Device dev)
    {
        bool   immediate = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_REWIND_command);
            AaruLogging.WriteLine(Localization.Immediate_0, immediate);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Immediate_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out immediate))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        immediate = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.Rewind(out ReadOnlySpan<byte> senseBuffer, immediate, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_REWIND_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.REWIND_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REWIND_sense);

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

    static void Space(string devPath, Device dev)
    {
        SscSpaceCodes what  = SscSpaceCodes.LogicalBlock;
        int           count = -1;
        string        strDev;
        int           item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SPACE_command);
            AaruLogging.WriteLine(Localization.What_to_space_0, what);
            AaruLogging.WriteLine(Localization.How_many_0,      count);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.What_to_space);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4_5,
                                          SscSpaceCodes.LogicalBlock,
                                          SscSpaceCodes.Filemark,
                                          SscSpaceCodes.SequentialFilemark,
                                          SscSpaceCodes.EndOfData,
                                          SscSpaceCodes.Obsolete1,
                                          SscSpaceCodes.Obsolete2);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out what))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_space_type_Press_any_key_to_continue);
                        what = SscSpaceCodes.LogicalBlock;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_negative_for_reverse_Q);
                    strDev = Console.ReadLine();

                    if(!int.TryParse(strDev, out count))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        count = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.Space(out ReadOnlySpan<byte> senseBuffer, what, count, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SPACE_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.SPACE_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SPACE_sense);

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

    static void TrackSelect(string devPath, Device dev)
    {
        byte   track = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_TRACK_SELECT_command);
            AaruLogging.WriteLine(Localization.Track_0, track);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Track_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out track))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        track = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.TrackSelect(out ReadOnlySpan<byte> senseBuffer, track, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_TRACK_SELECT_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.TRACK_SELECT_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Streaming_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Streaming_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.TRACK_SELECT_sense);

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