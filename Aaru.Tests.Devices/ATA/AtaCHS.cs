// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AtaCHS.cs
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.ATA;

static class AtaChs
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_CHS_ATA_command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_IDENTIFY_DEVICE_command);
            AaruLogging.WriteLine(Localization._2_Send_READ_DMA_command);
            AaruLogging.WriteLine(Localization._3_Send_READ_DMA_WITH_RETRIES_command);
            AaruLogging.WriteLine(Localization._4_Send_READ_LONG_command);
            AaruLogging.WriteLine(Localization._5_Send_READ_LONG_WITH_RETRIES_command);
            AaruLogging.WriteLine(Localization._6_Send_READ_MULTIPLE_command);
            AaruLogging.WriteLine(Localization._7_Send_READ_SECTORS_command);
            AaruLogging.WriteLine(Localization._8_Send_READ_SECTORS_WITH_RETRIES_command);
            AaruLogging.WriteLine(Localization._9_Send_SEEK_command);
            AaruLogging.WriteLine(Localization.Send_SET_FEATURES_command);
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
                    Identify(devPath, dev);

                    continue;
                case 2:
                    ReadDma(devPath, dev, false);

                    continue;
                case 3:
                    ReadDma(devPath, dev, true);

                    continue;
                case 4:
                    ReadLong(devPath, dev, false);

                    continue;
                case 5:
                    ReadLong(devPath, dev, true);

                    continue;
                case 6:
                    ReadMultiple(devPath, dev);

                    continue;
                case 7:
                    ReadSectors(devPath, dev, false);

                    continue;
                case 8:
                    ReadSectors(devPath, dev, true);

                    continue;
                case 9:
                    Seek(devPath, dev);

                    continue;
                case 10:
                    SetFeatures(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void Identify(string devPath, Device dev)
    {
    start:
        Console.Clear();

        bool sense = dev.AtaIdentify(out byte[] buffer, out AtaErrorRegistersChs errorRegisters, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_IDENTIFY_DEVICE_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization._2_Decode_buffer);
        AaruLogging.WriteLine(Localization._3_Decode_error_registers);
        AaruLogging.WriteLine(Localization._4_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.IDENTIFY_DEVICE_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.IDENTIFY_DEVICE_decoded_response);

                if(buffer != null) AaruLogging.WriteLine(Decoders.ATA.Identify.Prettify(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.IDENTIFY_DEVICE_status_registers);
                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void ReadDma(string devPath, Device dev, bool retries)
    {
        ushort cylinder = 0;
        byte   head     = 0;
        byte   sector   = 1;
        byte   count    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);

            AaruLogging.WriteLine(retries
                                      ? Localization.Parameters_for_READ_DMA_WITH_RETRIES_command
                                      : Localization.Parameters_for_READ_DMA_command);

            AaruLogging.WriteLine(Localization.Cylinder_0, cylinder);
            AaruLogging.WriteLine(Localization.Head_0,     head);
            AaruLogging.WriteLine(Localization.Sector_0,   sector);
            AaruLogging.WriteLine(Localization.Count_0,    count);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_sectors);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
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

        bool sense = dev.ReadDma(out byte[] buffer,
                                 out AtaErrorRegistersChs errorRegisters,
                                 retries,
                                 cylinder,
                                 head,
                                 sector,
                                 count,
                                 dev.Timeout,
                                 out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);

        AaruLogging.WriteLine(retries
                                  ? Localization.Sending_READ_DMA_WITH_RETRIES_to_the_device
                                  : Localization.Sending_READ_DMA_to_the_device);

        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization.Decode_error_registers);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization._1_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_DMA_WITH_RETRIES_response
                                          : Localization.READ_DMA_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_DMA_WITH_RETRIES_status_registers
                                          : Localization.READ_DMA_status_registers);

                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void ReadLong(string devPath, Device dev, bool retries)
    {
        ushort cylinder  = 0;
        byte   head      = 0;
        byte   sector    = 1;
        uint   blockSize = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);

            AaruLogging.WriteLine(retries
                                      ? Localization.Parameters_for_READ_LONG_WITH_RETRIES_command
                                      : Localization.Parameters_for_READ_LONG_command);

            AaruLogging.WriteLine(Localization.Cylinder_0,   cylinder);
            AaruLogging.WriteLine(Localization.Head_0,       head);
            AaruLogging.WriteLine(Localization.Sector_0,     sector);
            AaruLogging.WriteLine(Localization.Block_size_0, blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_bytes_to_expect);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadLong(out byte[] buffer,
                                  out AtaErrorRegistersChs errorRegisters,
                                  retries,
                                  cylinder,
                                  head,
                                  sector,
                                  blockSize,
                                  dev.Timeout,
                                  out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);

        AaruLogging.WriteLine(retries
                                  ? Localization.Sending_READ_LONG_WITH_RETRIES_to_the_device
                                  : Localization.Sending_READ_LONG_to_the_device);

        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization.Decode_error_registers);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization._1_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_LONG_WITH_RETRIES_response
                                          : Localization.READ_LONG_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_LONG_WITH_RETRIES_status_registers
                                          : Localization.READ_LONG_status_registers);

                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void ReadMultiple(string devPath, Device dev)
    {
        ushort cylinder = 0;
        byte   head     = 0;
        byte   sector   = 1;
        byte   count    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_MULTIPLE_command);
            AaruLogging.WriteLine(Localization.Cylinder_0, cylinder);
            AaruLogging.WriteLine(Localization.Head_0,     head);
            AaruLogging.WriteLine(Localization.Sector_0,   sector);
            AaruLogging.WriteLine(Localization.Count_0,    count);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_sectors);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
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

        bool sense = dev.ReadMultiple(out byte[] buffer,
                                      out AtaErrorRegistersChs errorRegisters,
                                      cylinder,
                                      head,
                                      sector,
                                      count,
                                      dev.Timeout,
                                      out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_MULTIPLE_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization.Decode_error_registers);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization._1_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_MULTIPLE_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_MULTIPLE_status_registers);
                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void ReadSectors(string devPath, Device dev, bool retries)
    {
        ushort cylinder = 0;
        byte   head     = 0;
        byte   sector   = 1;
        byte   count    = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);

            AaruLogging.WriteLine(retries
                                      ? Localization.Parameters_for_READ_SECTORS_WITH_RETRIES_command
                                      : Localization.Parameters_for_READ_SECTORS_command);

            AaruLogging.WriteLine(Localization.Cylinder_0, cylinder);
            AaruLogging.WriteLine(Localization.Head_0,     head);
            AaruLogging.WriteLine(Localization.Sector_0,   sector);
            AaruLogging.WriteLine(Localization.Count_0,    count);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.How_many_sectors);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out count))
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

        bool sense = dev.Read(out byte[] buffer,
                              out AtaErrorRegistersChs errorRegisters,
                              retries,
                              cylinder,
                              head,
                              sector,
                              count,
                              dev.Timeout,
                              out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);

        AaruLogging.WriteLine(retries
                                  ? Localization.Sending_READ_SECTORS_WITH_RETRIES_to_the_device
                                  : Localization.Sending_READ_SECTORS_to_the_device);

        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0, sense);
        AaruLogging.WriteLine(Localization.Buffer_is_0_bytes, buffer?.Length.ToString() ?? Localization._null);
        AaruLogging.WriteLine(Localization.Buffer_is_null_or_empty_0_Q, ArrayHelpers.ArrayIsNullOrEmpty(buffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization.Print_buffer);
        AaruLogging.WriteLine(Localization.Decode_error_registers);
        AaruLogging.WriteLine(Localization.Send_command_again);
        AaruLogging.WriteLine(Localization._1_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_SECTORS_WITH_RETRIES_response
                                          : Localization.READ_SECTORS_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                AaruLogging.WriteLine(retries
                                          ? Localization.READ_SECTORS_WITH_RETRIES_status_registers
                                          : Localization.READ_SECTORS_status_registers);

                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void Seek(string devPath, Device dev)
    {
        ushort cylinder = 0;
        byte   head     = 0;
        byte   sector   = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SEEK_command);
            AaruLogging.WriteLine(Localization.Cylinder_0, cylinder);
            AaruLogging.WriteLine(Localization.Head_0,     head);
            AaruLogging.WriteLine(Localization.Sector_0,   sector);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.Seek(out AtaErrorRegistersChs errorRegisters,
                              cylinder,
                              head,
                              sector,
                              dev.Timeout,
                              out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SEEK_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Decode_error_registers);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SEEK_status_registers);
                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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

    static void SetFeatures(string devPath, Device dev)
    {
        ushort cylinder    = 0;
        byte   head        = 0;
        byte   sector      = 0;
        byte   feature     = 0;
        byte   sectorCount = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_SET_FEATURES_command);
            AaruLogging.WriteLine(Localization.Cylinder_0,     cylinder);
            AaruLogging.WriteLine(Localization.Head_0,         head);
            AaruLogging.WriteLine(Localization.Sector_0,       sector);
            AaruLogging.WriteLine(Localization.Sector_count_0, sectorCount);
            AaruLogging.WriteLine(Localization.Feature_0_X2,   feature);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.What_cylinder);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out cylinder))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        cylinder = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.What_head);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out head))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        head = 0;
                        Console.ReadKey();

                        continue;
                    }

                    if(head > 15)
                    {
                        AaruLogging.WriteLine(Localization.Head_cannot_be_bigger_than_15_Setting_it_to_15);
                        head = 15;
                    }

                    AaruLogging.Write(Localization.What_sector);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sector))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sector = 0;
                        Console.ReadKey();
                    }

                    AaruLogging.Write(Localization.What_sector_count);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out sectorCount))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        sectorCount = 0;
                        Console.ReadKey();
                    }

                    AaruLogging.Write(Localization.What_feature);
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

        bool sense = dev.Seek(out AtaErrorRegistersChs errorRegisters,
                              cylinder,
                              head,
                              sector,
                              dev.Timeout,
                              out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_SET_FEATURES_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Decode_error_registers);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_CHS_ATA_commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_CHS_ATA_commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.SET_FEATURES_status_registers);
                AaruLogging.Write(MainClass.DecodeAtaRegisters(errorRegisters));
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