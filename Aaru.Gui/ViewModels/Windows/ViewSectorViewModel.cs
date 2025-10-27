// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ViewSectorViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the sector viewing window.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;

namespace Aaru.Gui.ViewModels.Windows;

public sealed partial class ViewSectorViewModel : ViewModelBase
{
    const    int         HEX_COLUMNS = 32;
    readonly IMediaImage _inputFormat;
    [ObservableProperty]
    bool _longSectorChecked;
    [ObservableProperty]
    bool _longSectorVisible;
    [ObservableProperty]
    string _printHexText;
    double _sectorNumber;
    [ObservableProperty]
    string _totalSectorsText;

    // TODO: Show message when sector was not dumped
    public ViewSectorViewModel([NotNull] IMediaImage inputFormat)
    {
        _inputFormat = inputFormat;

        ErrorNumber errno = inputFormat.ReadSectorLong(0, false, out _, out _);

        LongSectorVisible = errno == ErrorNumber.NoError;
        TotalSectorsText  = $"of {inputFormat.Info.Sectors}";
        SectorNumber      = 0;
    }

    public bool LongSectorChecked
    {
        get => _longSectorChecked;
        set
        {
            SetProperty(ref _longSectorChecked, value);

            ErrorNumber errno = LongSectorChecked
                                    ? _inputFormat.ReadSectorLong((ulong)SectorNumber, false, out byte[] sector, out _)
                                    : _inputFormat.ReadSector((ulong)SectorNumber, false, out sector, out _);

            if(errno != ErrorNumber.NoError) return;

            SectorData = sector;
            ColorSector();
        }
    }

    public double SectorNumber
    {
        get => _sectorNumber;
        set
        {
            SetProperty(ref _sectorNumber, value);

            ErrorNumber errno = LongSectorChecked
                                    ? _inputFormat.ReadSectorLong((ulong)SectorNumber, false, out byte[] sector, out _)
                                    : _inputFormat.ReadSector((ulong)SectorNumber, false, out sector, out _);

            if(errno == ErrorNumber.NoError) PrintHexText = PrintHex.ByteArrayToHexArrayString(sector, HEX_COLUMNS);
        }
    }
}