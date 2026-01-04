// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FluxInfoViewModel.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the Flux information tab.
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
// Copyright © 2011-2025 Rebecca Wallander
// ****************************************************************************/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aaru.CommonTypes.Structs;
using Aaru.Gui.Models;

namespace Aaru.Gui.ViewModels.Tabs;

public sealed class FluxInfoViewModel : ViewModelBase
{
    public FluxInfoViewModel(List<FluxCapture> fluxCaptures)
    {
        FluxCaptures = [];

        if(fluxCaptures is { Count: > 0 })
        {
            foreach(FluxCapture capture in fluxCaptures)
            {
                FluxCaptures.Add(new FluxCaptureModel
                {
                    Head            = capture.Head,
                    Track           = capture.Track,
                    SubTrack        = capture.SubTrack,
                    CaptureIndex    = capture.CaptureIndex,
                    IndexResolution = capture.IndexResolution,
                    DataResolution  = capture.DataResolution
                });
            }
        }
    }

    public ObservableCollection<FluxCaptureModel> FluxCaptures { get; }
}

