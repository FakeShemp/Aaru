// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MMC.cs
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
using Aaru.Decoders.CD;
using Aaru.Decoders.SCSI;
using Aaru.Decoders.SCSI.MMC;
using Aaru.Devices;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Tests.Devices.SCSI;

static class Mmc
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_MultiMedia_Command_to_the_device);
            AaruLogging.WriteLine(Localization.Send_GET_CONFIGURATION_command);
            AaruLogging.WriteLine(Localization.Send_PREVENT_ALLOW_MEDIUM_REMOVAL_command);
            AaruLogging.WriteLine(Localization.Send_READ_CD_command);
            AaruLogging.WriteLine(Localization.Send_READ_CD_MSF_command);
            AaruLogging.WriteLine(Localization.Send_READ_DISC_INFORMATION_command);
            AaruLogging.WriteLine(Localization.Send_READ_DISC_STRUCTURE_command);
            AaruLogging.WriteLine(Localization.Send_READ_TOC_PMA_ATIP_command);
            AaruLogging.WriteLine(Localization.Send_START_STOP_UNIT_command);
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
                    GetConfiguration(devPath, dev);

                    continue;
                case 2:
                    PreventAllowMediumRemoval(devPath, dev);

                    continue;
                case 3:
                    ReadCd(devPath, dev);

                    continue;
                case 4:
                    ReadCdMsf(devPath, dev);

                    continue;
                case 5:
                    ReadDiscInformation(devPath, dev);

                    continue;
                case 6:
                    ReadDiscStructure(devPath, dev);

                    continue;
                case 7:
                    ReadTocPmaAtip(devPath, dev);

                    continue;
                case 8:
                    StartStopUnit(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void GetConfiguration(string devPath, Device dev)
    {
        MmcGetConfigurationRt rt                    = MmcGetConfigurationRt.All;
        ushort                startingFeatureNumber = 0;
        string                strDev;
        int                   item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_GET_CONFIGURATION_command);
            AaruLogging.WriteLine(Localization.RT_0,             rt);
            AaruLogging.WriteLine(Localization.Feature_number_0, startingFeatureNumber);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.RT);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          MmcGetConfigurationRt.All,
                                          MmcGetConfigurationRt.Current,
                                          MmcGetConfigurationRt.Reserved,
                                          MmcGetConfigurationRt.Single);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out rt))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_object_type_Press_any_key_to_continue);
                        rt = MmcGetConfigurationRt.All;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Feature_number);
                    strDev = Console.ReadLine();

                    if(!ushort.TryParse(strDev, out startingFeatureNumber))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        startingFeatureNumber = 1;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.GetConfiguration(out byte[] buffer,
                                          out ReadOnlySpan<byte> senseBuffer,
                                          startingFeatureNumber,
                                          rt,
                                          dev.Timeout,
                                          out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_GET_CONFIGURATION_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.GET_CONFIGURATION_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.GET_CONFIGURATION_decoded_buffer);

                if(buffer != null)
                {
                    Features.SeparatedFeatures ftr = Features.Separate(buffer);
                    AaruLogging.WriteLine(Localization.GET_CONFIGURATION_length_is_0_bytes,       ftr.DataLength);
                    AaruLogging.WriteLine(Localization.GET_CONFIGURATION_current_profile_is_0_X4, ftr.CurrentProfile);

                    if(ftr.Descriptors != null)
                    {
                        foreach(Features.FeatureDescriptor desc in ftr.Descriptors)
                        {
                            AaruLogging.WriteLine(Localization.Feature_0_X4, desc.Code);

                            switch(desc.Code)
                            {
                                case 0x0000:
                                    AaruLogging.Write(Features.Prettify_0000(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0001:
                                    AaruLogging.Write(Features.Prettify_0001(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0002:
                                    AaruLogging.Write(Features.Prettify_0002(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0003:
                                    AaruLogging.Write(Features.Prettify_0003(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0004:
                                    AaruLogging.Write(Features.Prettify_0004(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0010:
                                    AaruLogging.Write(Features.Prettify_0010(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x001D:
                                    AaruLogging.Write(Features.Prettify_001D(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x001E:
                                    AaruLogging.Write(Features.Prettify_001E(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x001F:
                                    AaruLogging.Write(Features.Prettify_001F(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0020:
                                    AaruLogging.Write(Features.Prettify_0020(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0021:
                                    AaruLogging.Write(Features.Prettify_0021(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0022:
                                    AaruLogging.Write(Features.Prettify_0022(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0023:
                                    AaruLogging.Write(Features.Prettify_0023(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0024:
                                    AaruLogging.Write(Features.Prettify_0024(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0025:
                                    AaruLogging.Write(Features.Prettify_0025(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0026:
                                    AaruLogging.Write(Features.Prettify_0026(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0027:
                                    AaruLogging.Write(Features.Prettify_0027(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0028:
                                    AaruLogging.Write(Features.Prettify_0028(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0029:
                                    AaruLogging.Write(Features.Prettify_0029(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002A:
                                    AaruLogging.Write(Features.Prettify_002A(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002B:
                                    AaruLogging.Write(Features.Prettify_002B(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002C:
                                    AaruLogging.Write(Features.Prettify_002C(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002D:
                                    AaruLogging.Write(Features.Prettify_002D(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002E:
                                    AaruLogging.Write(Features.Prettify_002E(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x002F:
                                    AaruLogging.Write(Features.Prettify_002F(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0030:
                                    AaruLogging.Write(Features.Prettify_0030(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0031:
                                    AaruLogging.Write(Features.Prettify_0031(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0032:
                                    AaruLogging.Write(Features.Prettify_0032(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0033:
                                    AaruLogging.Write(Features.Prettify_0033(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0035:
                                    AaruLogging.Write(Features.Prettify_0035(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0037:
                                    AaruLogging.Write(Features.Prettify_0037(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0038:
                                    AaruLogging.Write(Features.Prettify_0038(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x003A:
                                    AaruLogging.Write(Features.Prettify_003A(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x003B:
                                    AaruLogging.Write(Features.Prettify_003B(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0040:
                                    AaruLogging.Write(Features.Prettify_0040(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0041:
                                    AaruLogging.Write(Features.Prettify_0041(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0042:
                                    AaruLogging.Write(Features.Prettify_0042(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0050:
                                    AaruLogging.Write(Features.Prettify_0050(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0051:
                                    AaruLogging.Write(Features.Prettify_0051(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0080:
                                    AaruLogging.Write(Features.Prettify_0080(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0100:
                                    AaruLogging.Write(Features.Prettify_0100(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0101:
                                    AaruLogging.Write(Features.Prettify_0101(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0102:
                                    AaruLogging.Write(Features.Prettify_0102(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0103:
                                    AaruLogging.Write(Features.Prettify_0103(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0104:
                                    AaruLogging.Write(Features.Prettify_0104(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0105:
                                    AaruLogging.Write(Features.Prettify_0105(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0106:
                                    AaruLogging.Write(Features.Prettify_0106(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0107:
                                    AaruLogging.Write(Features.Prettify_0107(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0108:
                                    AaruLogging.Write(Features.Prettify_0108(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0109:
                                    AaruLogging.Write(Features.Prettify_0109(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x010A:
                                    AaruLogging.Write(Features.Prettify_010A(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x010B:
                                    AaruLogging.Write(Features.Prettify_010B(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x010C:
                                    AaruLogging.Write(Features.Prettify_010C(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x010D:
                                    AaruLogging.Write(Features.Prettify_010D(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x010E:
                                    AaruLogging.Write(Features.Prettify_010E(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0110:
                                    AaruLogging.Write(Features.Prettify_0110(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0113:
                                    AaruLogging.Write(Features.Prettify_0113(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                case 0x0142:
                                    AaruLogging.Write(Features.Prettify_0142(desc.Data));
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                                default:
                                    AaruLogging.WriteLine(Localization.Dont_know_how_to_decode_feature_0, desc.Code);
                                    PrintHex.PrintHexArray(desc.Data, 64);

                                    break;
                            }
                        }
                    }
                }

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.GET_CONFIGURATION_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.GET_CONFIGURATION_decoded_sense);

                if(senseBuffer != null) AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));

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

    static void PreventAllowMediumRemoval(string devPath, Device dev)
    {
        var    prevent    = false;
        var    persistent = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_PREVENT_ALLOW_MEDIUM_REMOVAL_command);
            AaruLogging.WriteLine(Localization.Prevent_removal_0,  prevent);
            AaruLogging.WriteLine(Localization.Persistent_value_0, persistent);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Prevent_removal_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out prevent))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        prevent = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Persistent_value_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out persistent))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        persistent = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.PreventAllowMediumRemoval(out ReadOnlySpan<byte> senseBuffer,
                                                   persistent,
                                                   prevent,
                                                   dev.Timeout,
                                                   out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_PREVENT_ALLOW_MEDIUM_REMOVAL_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.PREVENT_ALLOW_MEDIUM_REMOVAL_decoded_sense);
        AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.PREVENT_ALLOW_MEDIUM_REMOVAL_sense);

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

    static void ReadCd(string devPath, Device dev)
    {
        uint           address    = 0;
        uint           length     = 1;
        MmcSectorTypes sectorType = MmcSectorTypes.AllTypes;
        var            dap        = false;
        var            relative   = false;
        var            sync       = false;
        MmcHeaderCodes header     = MmcHeaderCodes.None;
        var            user       = true;
        var            edc        = false;
        MmcErrorField  c2         = MmcErrorField.None;
        MmcSubchannel  subchan    = MmcSubchannel.None;
        uint           blockSize  = 2352;
        string         strDev;
        int            item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CD_command);
            AaruLogging.WriteLine(Localization.Address_relative_to_current_position_0,    relative);
            AaruLogging.WriteLine(relative ? Localization.Address_0 : Localization.LBA_0, address);
            AaruLogging.WriteLine(Localization.Will_transfer_0_sectors,                   length);
            AaruLogging.WriteLine(Localization.Sector_type_0,                             sectorType);
            AaruLogging.WriteLine(Localization.Process_audio_0,                           dap);
            AaruLogging.WriteLine(Localization.Retrieve_sync_bytes_0,                     sync);
            AaruLogging.WriteLine(Localization.Header_mode_0,                             header);
            AaruLogging.WriteLine(Localization.Retrieve_user_data_0,                      user);
            AaruLogging.WriteLine(Localization.Retrieve_EDC_ECC_data_0,                   edc);
            AaruLogging.WriteLine(Localization.C2_mode_0,                                 c2);
            AaruLogging.WriteLine(Localization.Subchannel_mode_0,                         subchan);
            AaruLogging.WriteLine(Localization._0_bytes_per_sector,                       blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Address_is_relative_to_current_position);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        relative = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(relative ? Localization.Address_Q : Localization.LBA_Q);
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

                    AaruLogging.WriteLine(Localization.Sector_type);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4_5,
                                          MmcSectorTypes.AllTypes,
                                          MmcSectorTypes.Cdda,
                                          MmcSectorTypes.Mode1,
                                          MmcSectorTypes.Mode2,
                                          MmcSectorTypes.Mode2Form1,
                                          MmcSectorTypes.Mode2Form2);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out sectorType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_sector_type_Press_any_key_to_continue);
                        sectorType = MmcSectorTypes.AllTypes;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Process_audio_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dap))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        dap = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_sync_bytes_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sync))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sync = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Header_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          MmcHeaderCodes.None,
                                          MmcHeaderCodes.HeaderOnly,
                                          MmcHeaderCodes.SubHeaderOnly,
                                          MmcHeaderCodes.AllHeaders);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out header))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_header_mode_Press_any_key_to_continue);
                        header = MmcHeaderCodes.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_user_data_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out user))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        user = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_EDC_ECC_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out edc))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        edc = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.C2_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2,
                                          MmcErrorField.None,
                                          MmcErrorField.C2Pointers,
                                          MmcErrorField.C2PointersAndBlock);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out c2))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_C2_mode_Press_any_key_to_continue);
                        c2 = MmcErrorField.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Subchannel_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          MmcSubchannel.None,
                                          MmcSubchannel.Raw,
                                          MmcSubchannel.Q16,
                                          MmcSubchannel.Rw);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out subchan))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_subchannel_mode_Press_any_key_to_continue);
                        subchan = MmcSubchannel.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Expected_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 2352;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadCd(out byte[] buffer,
                                out ReadOnlySpan<byte> senseBuffer,
                                address,
                                blockSize,
                                length,
                                sectorType,
                                dap,
                                relative,
                                sync,
                                header,
                                user,
                                edc,
                                c2,
                                subchan,
                                dev.Timeout,
                                out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CD_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_decoded_sense);
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

    static void ReadCdMsf(string devPath, Device dev)
    {
        byte           startFrame  = 0;
        byte           startSecond = 2;
        byte           startMinute = 0;
        byte           endFrame    = 0;
        const byte     endSecond   = 0;
        byte           endMinute   = 0;
        MmcSectorTypes sectorType  = MmcSectorTypes.AllTypes;
        var            dap         = false;
        var            sync        = false;
        MmcHeaderCodes header      = MmcHeaderCodes.None;
        var            user        = true;
        var            edc         = false;
        MmcErrorField  c2          = MmcErrorField.None;
        MmcSubchannel  subchan     = MmcSubchannel.None;
        uint           blockSize   = 2352;
        string         strDev;
        int            item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CD_MSF_command);
            AaruLogging.WriteLine(Localization.Start_0_1_2,             startMinute, startSecond, startFrame);
            AaruLogging.WriteLine(Localization.End_0_1_2,               endMinute,   endSecond,   endFrame);
            AaruLogging.WriteLine(Localization.Sector_type_0,           sectorType);
            AaruLogging.WriteLine(Localization.Process_audio_0,         dap);
            AaruLogging.WriteLine(Localization.Retrieve_sync_bytes_0,   sync);
            AaruLogging.WriteLine(Localization.Header_mode_0,           header);
            AaruLogging.WriteLine(Localization.Retrieve_user_data_0,    user);
            AaruLogging.WriteLine(Localization.Retrieve_EDC_ECC_data_0, edc);
            AaruLogging.WriteLine(Localization.C2_mode_0,               c2);
            AaruLogging.WriteLine(Localization.Subchannel_mode_0,       subchan);
            AaruLogging.WriteLine(Localization._0_bytes_per_sector,     blockSize);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

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

                    AaruLogging.WriteLine(Localization.Sector_type);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3_4_5,
                                          MmcSectorTypes.AllTypes,
                                          MmcSectorTypes.Cdda,
                                          MmcSectorTypes.Mode1,
                                          MmcSectorTypes.Mode2,
                                          MmcSectorTypes.Mode2Form1,
                                          MmcSectorTypes.Mode2Form2);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out sectorType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_sector_type_Press_any_key_to_continue);
                        sectorType = MmcSectorTypes.AllTypes;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Process_audio_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dap))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        dap = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_sync_bytes_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out sync))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        sync = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Header_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          MmcHeaderCodes.None,
                                          MmcHeaderCodes.HeaderOnly,
                                          MmcHeaderCodes.SubHeaderOnly,
                                          MmcHeaderCodes.AllHeaders);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out header))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_header_mode_Press_any_key_to_continue);
                        header = MmcHeaderCodes.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_user_data_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out user))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        user = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Retrieve_EDC_ECC_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out edc))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        edc = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.C2_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2,
                                          MmcErrorField.None,
                                          MmcErrorField.C2Pointers,
                                          MmcErrorField.C2PointersAndBlock);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out c2))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_C2_mode_Press_any_key_to_continue);
                        c2 = MmcErrorField.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Subchannel_mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          MmcSubchannel.None,
                                          MmcSubchannel.Raw,
                                          MmcSubchannel.Q16,
                                          MmcSubchannel.Rw);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out subchan))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_subchannel_mode_Press_any_key_to_continue);
                        subchan = MmcSubchannel.None;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Expected_block_size_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out blockSize))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        blockSize = 2352;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        var startMsf = (uint)((startMinute << 16) + (startSecond << 8) + startFrame);
        var endMsf   = (uint)((startMinute << 16) + (startSecond << 8) + startFrame);
        Console.Clear();

        bool sense = dev.ReadCdMsf(out byte[] buffer,
                                   out ReadOnlySpan<byte> senseBuffer,
                                   startMsf,
                                   endMsf,
                                   blockSize,
                                   sectorType,
                                   dap,
                                   sync,
                                   header,
                                   user,
                                   edc,
                                   c2,
                                   subchan,
                                   dev.Timeout,
                                   out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CD_MSF_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_MSF_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_MSF_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CD_MSF_decoded_sense);
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

    static void ReadDiscInformation(string devPath, Device dev)
    {
        MmcDiscInformationDataTypes info = MmcDiscInformationDataTypes.DiscInformation;
        string                      strDev;
        int                         item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_DISC_INFORMATION_command);
            AaruLogging.WriteLine(Localization.Information_type_0, info);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Information_type);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2,
                                          MmcDiscInformationDataTypes.DiscInformation,
                                          MmcDiscInformationDataTypes.TrackResources,
                                          MmcDiscInformationDataTypes.PowResources);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out info))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_information_type_Press_any_key_to_continue);
                        info = MmcDiscInformationDataTypes.DiscInformation;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadDiscInformation(out byte[] buffer,
                                             out ReadOnlySpan<byte> senseBuffer,
                                             info,
                                             dev.Timeout,
                                             out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_DISC_INFORMATION_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_INFORMATION_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_INFORMATION_decoded_response);
                AaruLogging.Write(DiscInformation.Prettify(buffer));
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_INFORMATION_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_INFORMATION_decoded_sense);
                AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
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

    static void ReadDiscStructure(string devPath, Device dev)
    {
        MmcDiscStructureMediaType mediaType = MmcDiscStructureMediaType.Dvd;
        MmcDiscStructureFormat    format    = MmcDiscStructureFormat.CapabilityList;
        uint                      address   = 0;
        byte                      layer     = 0;
        byte                      agid      = 0;
        string                    strDev;
        int                       item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_DISC_STRUCTURE_command);
            AaruLogging.WriteLine(Localization.Media_type_0, mediaType);
            AaruLogging.WriteLine(Localization.Format_0,     format);
            AaruLogging.WriteLine(Localization.Address_0,    address);
            AaruLogging.WriteLine(Localization.Layer_0,      layer);
            AaruLogging.WriteLine(Localization.AGID_0,       agid);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Media_type);

                    AaruLogging.WriteLine(Localization.Available_values_0_1,
                                          MmcDiscStructureMediaType.Dvd,
                                          MmcDiscStructureMediaType.Bd);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out mediaType))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_media_type_Press_any_key_to_continue);
                        mediaType = MmcDiscStructureMediaType.Dvd;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Format);
                    AaruLogging.WriteLine(Localization.Available_values);

                    switch(mediaType)
                    {
                        case MmcDiscStructureMediaType.Dvd:
                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.PhysicalInformation,
                                                  MmcDiscStructureFormat.CopyrightInformation,
                                                  MmcDiscStructureFormat.DiscKey,
                                                  MmcDiscStructureFormat.BurstCuttingArea);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.DiscManufacturingInformation,
                                                  MmcDiscStructureFormat.SectorCopyrightInformation,
                                                  MmcDiscStructureFormat.MediaIdentifier,
                                                  MmcDiscStructureFormat.MediaKeyBlock);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.DvdramDds,
                                                  MmcDiscStructureFormat.DvdramMediumStatus,
                                                  MmcDiscStructureFormat.DvdramSpareAreaInformation,
                                                  MmcDiscStructureFormat.DvdramRecordingType);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.LastBorderOutRmd,
                                                  MmcDiscStructureFormat.SpecifiedRmd,
                                                  MmcDiscStructureFormat.PreRecordedInfo,
                                                  MmcDiscStructureFormat.DvdrMediaIdentifier);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.DvdrPhysicalInformation,
                                                  MmcDiscStructureFormat.Adip,
                                                  MmcDiscStructureFormat.HddvdCopyrightInformation,
                                                  MmcDiscStructureFormat.DvdAacs);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.HddvdrMediumStatus,
                                                  MmcDiscStructureFormat.HddvdrLastRmd,
                                                  MmcDiscStructureFormat.DvdrLayerCapacity,
                                                  MmcDiscStructureFormat.MiddleZoneStart);

                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.JumpIntervalSize,
                                                  MmcDiscStructureFormat.ManualLayerJumpStartLba,
                                                  MmcDiscStructureFormat.RemapAnchorPoint,
                                                  MmcDiscStructureFormat.Dcb);

                            break;
                        case MmcDiscStructureMediaType.Bd:
                            AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                                  MmcDiscStructureFormat.DiscInformation,
                                                  MmcDiscStructureFormat.BdBurstCuttingArea,
                                                  MmcDiscStructureFormat.BdDds,
                                                  MmcDiscStructureFormat.CartridgeStatus);

                            AaruLogging.WriteLine("\t{0} {1} {2}",
                                                  MmcDiscStructureFormat.BdSpareAreaInformation,
                                                  MmcDiscStructureFormat.RawDfl,
                                                  MmcDiscStructureFormat.Pac);

                            break;
                    }

                    AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                          MmcDiscStructureFormat.AacsVolId,
                                          MmcDiscStructureFormat.AacsMediaSerial,
                                          MmcDiscStructureFormat.AacsMediaId,
                                          MmcDiscStructureFormat.Aacsmkb);

                    AaruLogging.WriteLine("\t{0} {1} {2} {3}",
                                          MmcDiscStructureFormat.AacsDataKeys,
                                          MmcDiscStructureFormat.AacslbaExtents,
                                          MmcDiscStructureFormat.Aacsmkbcprm,
                                          MmcDiscStructureFormat.RecognizedFormatLayers);

                    AaruLogging.WriteLine("\t{0} {1}",
                                          MmcDiscStructureFormat.WriteProtectionStatus,
                                          MmcDiscStructureFormat.CapabilityList);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out format))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_structure_format_Press_any_key_to_continue);
                        format = MmcDiscStructureFormat.CapabilityList;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Address_Q);
                    strDev = Console.ReadLine();

                    if(!uint.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Layer_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out layer))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        layer = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.AGID_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out agid))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        agid = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadDiscStructure(out byte[] buffer,
                                           out ReadOnlySpan<byte> senseBuffer,
                                           mediaType,
                                           address,
                                           layer,
                                           format,
                                           agid,
                                           dev.Timeout,
                                           out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_DISC_STRUCTURE_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_STRUCTURE_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                // TODO: Implement
                AaruLogging.WriteLine(Localization.READ_DISC_STRUCTURE_decoding_not_yet_implemented);
                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_STRUCTURE_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_DISC_STRUCTURE_decoded_sense);
                AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
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

    static void ReadTocPmaAtip(string devPath, Device dev)
    {
        var    msf     = false;
        byte   format  = 0;
        byte   session = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_TOC_PMA_ATIP_command);
            AaruLogging.WriteLine(Localization.Return_MSF_values_0, msf);
            AaruLogging.WriteLine(Localization.Format_byte_0,       format);
            AaruLogging.WriteLine(Localization.Session_0,           session);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Return_MSF_values_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out msf))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        msf = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Format_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out format))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        format = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Session_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out session))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        session = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadTocPmaAtip(out byte[] buffer,
                                        out ReadOnlySpan<byte> senseBuffer,
                                        msf,
                                        format,
                                        session,
                                        dev.Timeout,
                                        out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_TOC_PMA_ATIP_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_TOC_PMA_ATIP_buffer);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_TOC_PMA_ATIP_decoded_buffer);

                if(buffer != null)
                {
                    switch(format)
                    {
                        case 0:
                            AaruLogging.Write(TOC.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                        case 1:
                            AaruLogging.Write(Session.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                        case 2:
                            AaruLogging.Write(FullTOC.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                        case 3:
                            AaruLogging.Write(PMA.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                        case 4:
                            AaruLogging.Write(ATIP.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                        case 5:
                            AaruLogging.Write(CDTextOnLeadIn.Prettify(buffer));
                            PrintHex.PrintHexArray(buffer, 64);

                            break;
                    }
                }

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_TOC_PMA_ATIP_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_TOC_PMA_ATIP_decoded_sense);

                if(senseBuffer != null) AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));

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

    static void StartStopUnit(string devPath, Device dev)
    {
        var    immediate         = false;
        var    changeFormatLayer = false;
        var    loadEject         = false;
        var    start             = false;
        byte   formatLayer       = 0;
        byte   powerConditions   = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_START_STOP_UNIT_command);
            AaruLogging.WriteLine(Localization.Immediate_0,           immediate);
            AaruLogging.WriteLine(Localization.Change_format_layer_0, changeFormatLayer);
            AaruLogging.WriteLine(Localization.Eject_0,               loadEject);
            AaruLogging.WriteLine(Localization.Start_0,               start);
            AaruLogging.WriteLine(Localization.Format_layer_0,        formatLayer);
            AaruLogging.WriteLine(Localization.Power_conditions_0,    powerConditions);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Immediate_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out immediate))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        immediate = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Change_format_layer_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out changeFormatLayer))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        changeFormatLayer = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Eject_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out loadEject))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        loadEject = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Start_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out start))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        start = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Format_layer_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out formatLayer))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        formatLayer = 0;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Power_conditions_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out powerConditions))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        powerConditions = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.StartStopUnit(out ReadOnlySpan<byte> senseBuffer,
                                       immediate,
                                       formatLayer,
                                       powerConditions,
                                       changeFormatLayer,
                                       loadEject,
                                       start,
                                       dev.Timeout,
                                       out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_START_STOP_UNIT_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.START_STOP_UNIT_decoded_sense);
        AaruLogging.Write(Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_MultiMedia_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_MultiMedia_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.START_STOP_UNIT_sense);

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