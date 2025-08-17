// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Main.cs
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
using System.Linq;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Tests.Devices;

static partial class MainClass
{
    public static void Main()
    {
        AaruLogging.WriteLineEvent        += Console.WriteLine;
        AaruLogging.WriteEvent            += Console.Write;
        AaruLogging.ErrorEvent   += Console.Error.WriteLine;
        AaruLogging.DebugEvent   += Console.Error.WriteLine;
        AaruLogging.VerboseEvent += Console.WriteLine;

        DeviceInfo[] devices = Aaru.Devices.Device.ListDevices();

        if(devices == null || devices.Length == 0)
        {
            AaruLogging.WriteLine(Localization.No_known_devices_attached);

            return;
        }

        devices = devices.OrderBy(d => d.Path).ToArray();

        while(true)
        {
            Console.Clear();

            AaruLogging.WriteLine(Localization.Aaru_device_handling_tests);

            AaruLogging.WriteLine("{6,-8}|{0,-22}|{1,-16}|{2,-24}|{3,-24}|{4,-10}|{5,-10}",
                                  Localization.Path,
                                  Localization.Vendor,
                                  Localization.Model,
                                  Localization.Serial,
                                  Localization.Bus,
                                  Localization.Supported,
                                  Localization.Number);

            AaruLogging.WriteLine("{6,-8}|{0,-22}+{1,-16}+{2,-24}+{3,-24}+{4,-10}+{5,-10}",
                                  "----------------------",
                                  "----------------",
                                  "------------------------",
                                  "------------------------",
                                  "----------",
                                  "----------",
                                  "--------");

            for(int i = 0; i < devices.Length; i++)
            {
                AaruLogging.WriteLine("{6,-8}|{0,-22}|{1,-16}|{2,-24}|{3,-24}|{4,-10}|{5,-10}",
                                      devices[i].Path,
                                      devices[i].Vendor,
                                      devices[i].Model,
                                      devices[i].Serial,
                                      devices[i].Bus,
                                      devices[i].Supported,
                                      i + 1);
            }

            AaruLogging.Write(Localization.Please_choose_which_drive_to_test_zero_to_exit);
            string strDev = Console.ReadLine();

            if(!int.TryParse(strDev, out int item))
            {
                AaruLogging.WriteLine(Localization.Not_a_number_Press_any_key_to_continue);
                Console.ReadKey();

                continue;
            }

            if(item == 0)
            {
                AaruLogging.WriteLine(Localization.Exiting);

                return;
            }

            if(item > devices.Length)
            {
                AaruLogging.WriteLine(Localization.No_such_device_Press_any_key_to_continue);
                Console.ReadKey();

                continue;
            }

            Device(devices[item - 1].Path);
        }
    }
}