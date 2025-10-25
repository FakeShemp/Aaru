// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BlurayInfoViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the Blu-ray information tab.
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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Decoders.Bluray;
using Aaru.Decoders.SCSI.MMC;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace Aaru.Gui.ViewModels.Tabs;

public sealed class BlurayInfoViewModel
{
    readonly byte[] _burstCuttingArea;
    readonly byte[] _cartridgeStatus;
    readonly byte[] _dds;
    readonly byte[] _discInformation;
    readonly byte[] _pac;
    readonly byte[] _powResources;
    readonly byte[] _rawDfl;
    readonly byte[] _spareAreaInformation;
    readonly byte[] _trackResources;
    readonly Window _view;

    public BlurayInfoViewModel([CanBeNull] byte[] blurayDiscInformation,      [CanBeNull] byte[] blurayBurstCuttingArea,
                               [CanBeNull] byte[] blurayDds,                  [CanBeNull] byte[] blurayCartridgeStatus,
                               [CanBeNull] byte[] bluraySpareAreaInformation, [CanBeNull] byte[] blurayPowResources,
                               [CanBeNull] byte[] blurayTrackResources,       [CanBeNull] byte[] blurayRawDfl,
                               [CanBeNull] byte[] blurayPac,                  Window             view)
    {
        _view                             = view;
        _discInformation                  = blurayDiscInformation;
        _burstCuttingArea                 = blurayBurstCuttingArea;
        _dds                              = blurayDds;
        _cartridgeStatus                  = blurayCartridgeStatus;
        _spareAreaInformation             = bluraySpareAreaInformation;
        _powResources                     = blurayPowResources;
        _trackResources                   = blurayTrackResources;
        _rawDfl                           = blurayRawDfl;
        _pac                              = blurayPac;
        SaveBlurayDiscInformationCommand  = new AsyncRelayCommand(SaveBlurayDiscInformationAsync);
        SaveBlurayBurstCuttingAreaCommand = new AsyncRelayCommand(SaveBlurayBurstCuttingAreaAsync);
        SaveBlurayDdsCommand              = new AsyncRelayCommand(SaveBlurayDdsAsync);
        SaveBlurayCartridgeStatusCommand  = new AsyncRelayCommand(SaveBlurayCartridgeStatusAsync);

        SaveBluraySpareAreaInformationCommand = new AsyncRelayCommand(SaveBluraySpareAreaInformationAsync);

        SaveBlurayPowResourcesCommand   = new AsyncRelayCommand(SaveBlurayPowResourcesAsync);
        SaveBlurayTrackResourcesCommand = new AsyncRelayCommand(SaveBlurayTrackResourcesAsync);
        SaveBlurayRawDflCommand         = new AsyncRelayCommand(SaveBlurayRawDflAsync);
        SaveBlurayPacCommand            = new AsyncRelayCommand(SaveBlurayPacAsync);

        if(blurayDiscInformation != null)
        {
            SaveBlurayDiscInformationVisible = true;
            BlurayDiscInformationText        = DI.Prettify(blurayDiscInformation);
        }

        if(blurayBurstCuttingArea != null)
        {
            SaveBlurayBurstCuttingAreaVisible = true;
            BlurayBurstCuttingAreaText        = BCA.Prettify(blurayBurstCuttingArea);
        }

        if(blurayDds != null)
        {
            SaveBlurayDdsVisible = true;
            BlurayDdsText        = DDS.Prettify(blurayDds);
        }

        if(blurayCartridgeStatus != null)
        {
            SaveBlurayCartridgeStatusVisible = true;
            BlurayCartridgeStatusText        = Cartridge.Prettify(blurayCartridgeStatus);
        }

        if(bluraySpareAreaInformation != null)
        {
            SaveBluraySpareAreaInformationVisible = true;
            BluraySpareAreaInformationText        = Spare.Prettify(bluraySpareAreaInformation);
        }

        if(blurayPowResources != null)
        {
            SaveBlurayPowResourcesVisible = true;
            BlurayPowResourcesText        = DiscInformation.Prettify(blurayPowResources);
        }

        if(blurayTrackResources != null)
        {
            SaveBlurayTrackResourcesVisible = true;
            BlurayTrackResourcesText        = DiscInformation.Prettify(blurayTrackResources);
        }

        SaveBlurayRawDflVisible = blurayRawDfl != null;
        SaveBlurayPacVisible    = blurayPac    != null;
    }

    public string   BlurayDiscInformationText             { get; }
    public string   BlurayBurstCuttingAreaText            { get; }
    public string   BlurayDdsText                         { get; }
    public string   BlurayCartridgeStatusText             { get; }
    public string   BluraySpareAreaInformationText        { get; }
    public string   BlurayPowResourcesText                { get; }
    public string   BlurayTrackResourcesText              { get; }
    public ICommand SaveBlurayDiscInformationCommand      { get; }
    public ICommand SaveBlurayBurstCuttingAreaCommand     { get; }
    public ICommand SaveBlurayDdsCommand                  { get; }
    public ICommand SaveBlurayCartridgeStatusCommand      { get; }
    public ICommand SaveBluraySpareAreaInformationCommand { get; }
    public ICommand SaveBlurayPowResourcesCommand         { get; }
    public ICommand SaveBlurayTrackResourcesCommand       { get; }
    public ICommand SaveBlurayRawDflCommand               { get; }
    public ICommand SaveBlurayPacCommand                  { get; }
    public bool     SaveBlurayDiscInformationVisible      { get; }
    public bool     SaveBlurayBurstCuttingAreaVisible     { get; }
    public bool     SaveBlurayDdsVisible                  { get; }
    public bool     SaveBlurayCartridgeStatusVisible      { get; }
    public bool     SaveBluraySpareAreaInformationVisible { get; }
    public bool     SaveBlurayPowResourcesVisible         { get; }
    public bool     SaveBlurayTrackResourcesVisible       { get; }
    public bool     SaveBlurayRawDflVisible               { get; }
    public bool     SaveBlurayPacVisible                  { get; }

    async Task SaveElementAsync(byte[] data)
    {
        IStorageFile result = await _view.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new List<FilePickerFileType>
            {
                FilePickerFileTypes.Binary
            }
        });

        if(result is null) return;

        var saveFs = new FileStream(result.Path.AbsolutePath, FileMode.Create);
        saveFs.Write(data, 0, data.Length);

        saveFs.Close();
    }

    Task SaveBlurayDiscInformationAsync() => SaveElementAsync(_discInformation);

    Task SaveBlurayBurstCuttingAreaAsync() => SaveElementAsync(_burstCuttingArea);

    Task SaveBlurayDdsAsync() => SaveElementAsync(_dds);

    Task SaveBlurayCartridgeStatusAsync() => SaveElementAsync(_cartridgeStatus);

    Task SaveBluraySpareAreaInformationAsync() => SaveElementAsync(_spareAreaInformation);

    Task SaveBlurayPowResourcesAsync() => SaveElementAsync(_powResources);

    Task SaveBlurayTrackResourcesAsync() => SaveElementAsync(_trackResources);

    Task SaveBlurayRawDflAsync() => SaveElementAsync(_rawDfl);

    Task SaveBlurayPacAsync() => SaveElementAsync(_pac);
}