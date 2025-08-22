// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SPC.cs
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

static class Spc
{
    internal static void Menu(string devPath, Device dev)
    {
        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Send_a_SCSI_Primary_Commands_to_the_device);
            AaruLogging.WriteLine(Localization._1_Send_INQUIRY_command);
            AaruLogging.WriteLine(Localization._2_Send_INQUIRY_EPVD_command);
            AaruLogging.WriteLine(Localization._3_Send_MODE_SENSE_6_command);
            AaruLogging.WriteLine(Localization._4_Send_MODE_SENSE_10_command);
            AaruLogging.WriteLine(Localization._5_Send_PREVENT_ALLOW_MEDIUM_REMOVAL_command);
            AaruLogging.WriteLine(Localization._6_SEND_READ_CAPACITY_10_command);
            AaruLogging.WriteLine(Localization._7_Send_READ_CAPACITY_16_command);
            AaruLogging.WriteLine(Localization._8_Send_READ_MEDIA_SERIAL_NUMBER_command);
            AaruLogging.WriteLine(Localization._9_Send_REQUEST_SENSE_command);
            AaruLogging.WriteLine(Localization._10_Send_TEST_UNIT_READY_command);
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
                    Inquiry(devPath, dev);

                    continue;
                case 2:
                    InquiryEvpd(devPath, dev);

                    continue;
                case 3:
                    ModeSense6(devPath, dev);

                    continue;
                case 4:
                    ModeSense10(devPath, dev);

                    continue;
                case 5:
                    PreventAllowMediumRemoval(devPath, dev);

                    continue;
                case 6:
                    ReadCapacity10(devPath, dev);

                    continue;
                case 7:
                    ReadCapacity16(devPath, dev);

                    continue;
                case 8:
                    ReadMediaSerialNumber(devPath, dev);

                    continue;
                case 9:
                    RequestSense(devPath, dev);

                    continue;
                case 10:
                    TestUnitReady(devPath, dev);

                    continue;
                default:
                    AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                    Console.ReadKey();

                    continue;
            }
        }
    }

    static void Inquiry(string devPath, Device dev)
    {
    start:
        Console.Clear();

        bool sense = dev.ScsiInquiry(out byte[] buffer,
                                     out ReadOnlySpan<byte> senseBuffer,
                                     dev.Timeout,
                                     out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_INQUIRY_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_decoded_response);

                if(buffer != null) AaruLogging.WriteLine("{0}", Decoders.SCSI.Inquiry.Prettify(buffer));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_decoded_sense);
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

    static void InquiryEvpd(string devPath, Device dev)
    {
        byte   page = 1;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_INQUIRY_command);
            AaruLogging.WriteLine(Localization.EVPD_page_0, page);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Page_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out page))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        page = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ScsiInquiry(out byte[] buffer,
                                     out ReadOnlySpan<byte> senseBuffer,
                                     page,
                                     dev.Timeout,
                                     out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_INQUIRY_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.INQUIRY_decoded_sense);
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

    static void ModeSense6(string devPath, Device dev)
    {
        bool                     dbd         = false;
        ScsiModeSensePageControl pageControl = ScsiModeSensePageControl.Current;
        byte                     page        = 0x3F;
        byte                     subpage     = 0xFF;
        string                   strDev;
        int                      item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_MODE_SENSE_6_command);
            AaruLogging.WriteLine(Localization.DBD_0,          dbd);
            AaruLogging.WriteLine(Localization.Page_control_0, pageControl);
            AaruLogging.WriteLine(Localization.Page_0,         page);
            AaruLogging.WriteLine(Localization.Subpage_0,      subpage);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.DBD_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dbd))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        dbd = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Page_control);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          ScsiModeSensePageControl.Changeable,
                                          ScsiModeSensePageControl.Current,
                                          ScsiModeSensePageControl.Default,
                                          ScsiModeSensePageControl.Saved);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out pageControl))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_page_control_Press_any_key_to_continue);
                        pageControl = ScsiModeSensePageControl.Current;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Page_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out page))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        page = 0x3F;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Subpage_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out subpage))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        subpage = 0xFF;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ModeSense6(out byte[] buffer,
                                    out ReadOnlySpan<byte> senseBuffer,
                                    dbd,
                                    pageControl,
                                    page,
                                    subpage,
                                    dev.Timeout,
                                    out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_MODE_SENSE_6_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_6_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_6_decoded_response);

                if(buffer != null) AaruLogging.WriteLine("{0}", Modes.PrettifyModeHeader6(buffer, dev.ScsiType));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_6_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_6_decoded_sense);
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

    static void ModeSense10(string devPath, Device dev)
    {
        bool                     llba        = false;
        bool                     dbd         = false;
        ScsiModeSensePageControl pageControl = ScsiModeSensePageControl.Current;
        byte                     page        = 0x3F;
        byte                     subpage     = 0xFF;
        string                   strDev;
        int                      item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_MODE_SENSE_10_command);
            AaruLogging.WriteLine(Localization.LLBA_0,         llba);
            AaruLogging.WriteLine(Localization.DBD_0,          dbd);
            AaruLogging.WriteLine(Localization.Page_control_0, pageControl);
            AaruLogging.WriteLine(Localization.Page_0,         page);
            AaruLogging.WriteLine(Localization.Subpage_0,      subpage);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.LLBA_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out llba))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        llba = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.DBD_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out dbd))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        dbd = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.WriteLine(Localization.Page_control);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          ScsiModeSensePageControl.Changeable,
                                          ScsiModeSensePageControl.Current,
                                          ScsiModeSensePageControl.Default,
                                          ScsiModeSensePageControl.Saved);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out pageControl))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_page_control_Press_any_key_to_continue);
                        pageControl = ScsiModeSensePageControl.Current;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Page_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out page))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        page = 0x3F;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Subpage_Q);
                    strDev = Console.ReadLine();

                    if(!byte.TryParse(strDev, out subpage))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        subpage = 0xFF;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ModeSense10(out byte[] buffer,
                                     out ReadOnlySpan<byte> senseBuffer,
                                     llba,
                                     dbd,
                                     pageControl,
                                     page,
                                     subpage,
                                     dev.Timeout,
                                     out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_MODE_SENSE_10_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_10_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_10_decoded_response);

                if(buffer != null) AaruLogging.WriteLine("{0}", Modes.PrettifyModeHeader10(buffer, dev.ScsiType));

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_10_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 4:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.MODE_SENSE_10_decoded_sense);
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

    static void PreventAllowMediumRemoval(string devPath, Device dev)
    {
        ScsiPreventAllowMode mode = ScsiPreventAllowMode.Allow;
        string               strDev;
        int                  item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_PREVENT_ALLOW_MEDIUM_REMOVAL_command);
            AaruLogging.WriteLine(Localization.Mode_0, mode);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.WriteLine(Localization.Mode);

                    AaruLogging.WriteLine(Localization.Available_values_0_1_2_3,
                                          ScsiPreventAllowMode.Allow,
                                          ScsiPreventAllowMode.Prevent,
                                          ScsiPreventAllowMode.PreventAll,
                                          ScsiPreventAllowMode.PreventChanger);

                    AaruLogging.Write(Localization.Choose_Q);
                    strDev = Console.ReadLine();

                    if(!Enum.TryParse(strDev, true, out mode))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_correct_mode_Press_any_key_to_continue);
                        mode = ScsiPreventAllowMode.Allow;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense =
            dev.SpcPreventAllowMediumRemoval(out ReadOnlySpan<byte> senseBuffer,
                                             mode,
                                             dev.Timeout,
                                             out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_PREVENT_ALLOW_MEDIUM_REMOVAL_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);
        AaruLogging.WriteLine(Localization.PREVENT_ALLOW_MEDIUM_REMOVAL_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Send_command_again);
        AaruLogging.WriteLine(Localization._2_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

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

    static void ReadCapacity10(string devPath, Device dev)
    {
        bool   relative = false;
        bool   partial  = false;
        uint   address  = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CAPACITY_10_command);
            AaruLogging.WriteLine(Localization.Relative_address_0, relative);
            AaruLogging.WriteLine(Localization.Partial_capacity_0, partial);
            AaruLogging.WriteLine(Localization.Address_0,          address);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Relative_address_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out relative))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        relative = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Partial_capacity_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out partial))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        partial = false;
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
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadCapacity(out byte[] buffer,
                                      out ReadOnlySpan<byte> senseBuffer,
                                      relative,
                                      address,
                                      partial,
                                      dev.Timeout,
                                      out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CAPACITY_10_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_10_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_10_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_10_decoded_sense);
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

    static void ReadCapacity16(string devPath, Device dev)
    {
        bool   partial = false;
        ulong  address = 0;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_READ_CAPACITY_16_command);
            AaruLogging.WriteLine(Localization.Partial_capacity_0, partial);
            AaruLogging.WriteLine(Localization.Address_0,          address);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Partial_capacity_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out partial))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_boolean_Press_any_key_to_continue);
                        partial = false;
                        Console.ReadKey();

                        continue;
                    }

                    AaruLogging.Write(Localization.Address_Q);
                    strDev = Console.ReadLine();

                    if(!ulong.TryParse(strDev, out address))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        address = 0;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();

        bool sense = dev.ReadCapacity16(out byte[] buffer,
                                        out ReadOnlySpan<byte> senseBuffer,
                                        address,
                                        partial,
                                        dev.Timeout,
                                        out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_CAPACITY_16_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_16_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_16_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_CAPACITY_16_decoded_sense);
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

    static void ReadMediaSerialNumber(string devPath, Device dev)
    {
    start:
        Console.Clear();

        bool sense = dev.ReadMediaSerialNumber(out byte[] buffer,
                                               out ReadOnlySpan<byte> senseBuffer,
                                               dev.Timeout,
                                               out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_READ_MEDIA_SERIAL_NUMBER_to_the_device);
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
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_MEDIA_SERIAL_NUMBER_response);

                if(buffer != null) PrintHex.PrintHexArray(buffer, 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_MEDIA_SERIAL_NUMBER_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 3:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.READ_MEDIA_SERIAL_NUMBER_decoded_sense);
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

    static void RequestSense(string devPath, Device dev)
    {
        bool   descriptor = false;
        string strDev;
        int    item;

    parameters:

        while(true)
        {
            Console.Clear();
            AaruLogging.WriteLine(Localization.Device_0, devPath);
            AaruLogging.WriteLine(Localization.Parameters_for_REQUEST_SENSE_command);
            AaruLogging.WriteLine(Localization.Descriptor_0, descriptor);
            AaruLogging.WriteLine();
            AaruLogging.WriteLine(Localization.Choose_what_to_do);
            AaruLogging.WriteLine(Localization._1_Change_parameters);
            AaruLogging.WriteLine(Localization._2_Send_command_with_these_parameters);
            AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);

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
                    AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                    return;
                case 1:
                    AaruLogging.Write(Localization.Descriptor_Q);
                    strDev = Console.ReadLine();

                    if(!bool.TryParse(strDev, out descriptor))
                    {
                        AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                        descriptor = false;
                        Console.ReadKey();
                    }

                    break;
                case 2:
                    goto start;
            }
        }

    start:
        Console.Clear();
        bool sense = dev.RequestSense(descriptor, out byte[] senseBuffer, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_REQUEST_SENSE_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0,
                              ArrayHelpers.ArrayIsNullOrEmpty(senseBuffer));

        AaruLogging.WriteLine(Localization.REQUEST_SENSE_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization._3_Change_parameters);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.REQUEST_SENSE_sense);

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

    static void TestUnitReady(string devPath, Device dev)
    {
    start:
        Console.Clear();
        bool sense = dev.ScsiTestUnitReady(out ReadOnlySpan<byte> senseBuffer, dev.Timeout, out double duration);

    menu:
        AaruLogging.WriteLine(Localization.Device_0, devPath);
        AaruLogging.WriteLine(Localization.Sending_TEST_UNIT_READY_to_the_device);
        AaruLogging.WriteLine(Localization.Command_took_0_ms, duration);
        AaruLogging.WriteLine(Localization.Sense_is_0,        sense);

        AaruLogging.WriteLine(Localization.Sense_buffer_is_0_bytes, senseBuffer.Length.ToString());

        AaruLogging.WriteLine(Localization.Sense_buffer_is_null_or_empty_0, senseBuffer.IsEmpty);

        AaruLogging.WriteLine(Localization.TEST_UNIT_READY_decoded_sense);
        AaruLogging.Write("{0}", Sense.PrettifySense(senseBuffer.ToArray()));
        AaruLogging.WriteLine();
        AaruLogging.WriteLine(Localization.Choose_what_to_do);
        AaruLogging.WriteLine(Localization._1_Print_sense_buffer);
        AaruLogging.WriteLine(Localization._2_Send_command_again);
        AaruLogging.WriteLine(Localization.Return_to_SCSI_Primary_Commands_menu);
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
                AaruLogging.WriteLine(Localization.Returning_to_SCSI_Primary_Commands_menu);

                return;
            case 1:
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);
                AaruLogging.WriteLine(Localization.TEST_UNIT_READY_sense);

                if(senseBuffer != null) PrintHex.PrintHexArray(senseBuffer.ToArray(), 64);

                AaruLogging.WriteLine(Localization.Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();
                AaruLogging.WriteLine(Localization.Device_0, devPath);

                goto menu;
            case 2:
                goto start;
            default:
                AaruLogging.WriteLine(Localization.Incorrect_option_Press_any_key_to_continue);
                Console.ReadKey();
                Console.Clear();

                goto menu;
        }
    }
}