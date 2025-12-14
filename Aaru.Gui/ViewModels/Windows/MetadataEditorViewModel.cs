// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MetadataEditorViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Metadata editor window view model.
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.Gui.Helpers;
using Aaru.Gui.Localization;
using Aaru.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Sentry;
using File = System.IO.File;

namespace Aaru.Gui.ViewModels.Windows;

public sealed partial class MetadataEditorViewModel : ViewModelBase
{
    readonly Window _view;

    [ObservableProperty]
    ObservableCollection<AdvertisementViewModel> _advertisements = [];

    [ObservableProperty]
    ObservableCollection<Architecture> _architectures = [];

    [ObservableProperty]
    ObservableCollection<AudioMediaViewModel> _audioMedias = [];

    [ObservableProperty]
    ObservableCollection<string> _authors = [];

    // Complex object lists
    [ObservableProperty]
    ObservableCollection<BarcodeViewModel> _barcodes = [];

    [ObservableProperty]
    ObservableCollection<BlockMediaViewModel> _blockMedias = [];

    [ObservableProperty]
    ObservableCollection<BookViewModel> _books = [];

    [ObservableProperty]
    ObservableCollection<string> _categories = [];

    // String lists
    [ObservableProperty]
    ObservableCollection<string> _developers = [];

    [ObservableProperty]
    string _filePath;

    [ObservableProperty]
    ObservableCollection<string> _keywords = [];

    // Enum lists
    [ObservableProperty]
    ObservableCollection<LocalizedEnumValue<Language>> _languages = [];

    [ObservableProperty]
    ObservableCollection<LinearMediaViewModel> _linearMedias = [];

    [ObservableProperty]
    ObservableCollection<MagazineViewModel> _magazines = [];

    // Basic metadata fields
    [ObservableProperty]
    string _name;

    [ObservableProperty]
    ObservableCollection<OpticalDiscViewModel> _opticalDiscs = [];

    [ObservableProperty]
    string _partNumber;

    [ObservableProperty]
    ObservableCollection<PciViewModel> _pciCards = [];

    [ObservableProperty]
    ObservableCollection<string> _performers = [];

    [ObservableProperty]
    ObservableCollection<string> _publishers = [];

    [ObservableProperty]
    DateTime? _releaseDate;

    [ObservableProperty]
    ObservableCollection<RequiredOperatingSystemViewModel> _requiredOperatingSystems = [];

    [ObservableProperty]
    LocalizedEnumValue<ReleaseType> _selectedReleaseType;

    [ObservableProperty]
    string _serialNumber;

    [ObservableProperty]
    ObservableCollection<string> _subcategories = [];

    [ObservableProperty]
    ObservableCollection<string> _systems = [];

    [ObservableProperty]
    string _title;

    [ObservableProperty]
    ObservableCollection<UserManualViewModel> _userManuals = [];

    [ObservableProperty]
    string _version;

    public MetadataEditorViewModel(Window view, [CanBeNull] string existingFilePath = null)
    {
        _view     = view;
        _title    = existingFilePath == null ? GUI.Title_Create_Metadata : GUI.Title_Edit_Metadata;
        _filePath = existingFilePath;

        if(!string.IsNullOrEmpty(existingFilePath) && File.Exists(existingFilePath)) LoadMetadata(existingFilePath);
    }

    // Available enum values for ComboBoxes
    [NotNull]
    public IEnumerable<LocalizedEnumValue<ReleaseType>> AvailableReleaseTypes =>
        LocalizedEnumHelper.GetLocalizedValues<ReleaseType>();
    [NotNull]
    public IEnumerable<LocalizedEnumValue<Language>> AvailableLanguages =>
        LocalizedEnumHelper.GetLocalizedValues<Language>();
    [NotNull]
    public IEnumerable<Architecture> AvailableArchitectures => Enum.GetValues<Architecture>();
    [NotNull]
    public IEnumerable<BarcodeType> AvailableBarcodeTypes => Enum.GetValues<BarcodeType>();

    void LoadMetadata([NotNull] string path)
    {
        try
        {
            string       json         = File.ReadAllText(path);
            MetadataJson metadataJson = JsonSerializer.Deserialize(json, MetadataJsonContext.Default.MetadataJson);

            if(metadataJson?.AaruMetadata == null) return;

            Metadata metadata = metadataJson.AaruMetadata;

            // Basic fields
            Name    = metadata.Name;
            Version = metadata.Version;

            SelectedReleaseType = metadata.Release.HasValue
                                      ? new LocalizedEnumValue<ReleaseType>(metadata.Release.Value)
                                      : null;

            ReleaseDate  = metadata.ReleaseDate;
            PartNumber   = metadata.PartNumber;
            SerialNumber = metadata.SerialNumber;

            // String lists
            LoadStringList(metadata.Developers,    Developers);
            LoadStringList(metadata.Publishers,    Publishers);
            LoadStringList(metadata.Authors,       Authors);
            LoadStringList(metadata.Performers,    Performers);
            LoadStringList(metadata.Keywords,      Keywords);
            LoadStringList(metadata.Categories,    Categories);
            LoadStringList(metadata.Subcategories, Subcategories);
            LoadStringList(metadata.Systems,       Systems);

            // Enum lists
            if(metadata.Languages != null)
            {
                foreach(Language lang in metadata.Languages)
                    Languages.Add(new LocalizedEnumValue<Language>(lang));
            }

            LoadEnumList(metadata.Architectures, Architectures);

            // Complex objects
            if(metadata.Barcodes != null)
                foreach(Barcode barcode in metadata.Barcodes)
                    Barcodes.Add(new BarcodeViewModel(barcode));

            if(metadata.Magazines != null)
                foreach(Magazine magazine in metadata.Magazines)
                    Magazines.Add(new MagazineViewModel(magazine));

            if(metadata.Books != null)
                foreach(Book book in metadata.Books)
                    Books.Add(new BookViewModel(book));

            if(metadata.RequiredOperatingSystems != null)
            {
                foreach(RequiredOperatingSystem os in metadata.RequiredOperatingSystems)
                    RequiredOperatingSystems.Add(new RequiredOperatingSystemViewModel(os));
            }

            if(metadata.UserManuals != null)
                foreach(UserManual manual in metadata.UserManuals)
                    UserManuals.Add(new UserManualViewModel(manual));

            if(metadata.OpticalDiscs != null)
                foreach(OpticalDisc disc in metadata.OpticalDiscs)
                    OpticalDiscs.Add(new OpticalDiscViewModel(disc));

            if(metadata.Advertisements != null)
                foreach(Advertisement ad in metadata.Advertisements)
                    Advertisements.Add(new AdvertisementViewModel(ad));

            if(metadata.LinearMedias != null)
                foreach(LinearMedia media in metadata.LinearMedias)
                    LinearMedias.Add(new LinearMediaViewModel(media));

            if(metadata.PciCards != null)
                foreach(Pci pci in metadata.PciCards)
                    PciCards.Add(new PciViewModel(pci));

            if(metadata.BlockMedias != null)
                foreach(BlockMedia media in metadata.BlockMedias)
                    BlockMedias.Add(new BlockMediaViewModel(media));

            if(metadata.AudioMedias != null)
                foreach(AudioMedia media in metadata.AudioMedias)
                    AudioMedias.Add(new AudioMediaViewModel(media));
        }
        catch(Exception ex)
        {
            _ = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                        string.Format(GUI.Error_Loading_metadata, ex.Message),
                                                        ButtonEnum.Ok,
                                                        Icon.Error)
                                 .ShowAsync();
        }
    }

    static void LoadStringList([CanBeNull] List<string> source, ObservableCollection<string> target)
    {
        if(source == null) return;

        target.Clear();
        foreach(string item in source) target.Add(item);
    }

    static void LoadEnumList<T>([CanBeNull] List<T> source, ObservableCollection<T> target) where T : struct, Enum
    {
        if(source == null) return;

        target.Clear();
        foreach(T item in source) target.Add(item);
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        try
        {
            var metadata = new Metadata
            {
                Name          = Name,
                Version       = Version,
                Release       = SelectedReleaseType?.Value,
                ReleaseDate   = ReleaseDate,
                PartNumber    = PartNumber,
                SerialNumber  = SerialNumber,
                Developers    = Developers.Any() ? [..Developers] : null,
                Publishers    = Publishers.Any() ? [..Publishers] : null,
                Authors       = Authors.Any() ? [..Authors] : null,
                Performers    = Performers.Any() ? [..Performers] : null,
                Keywords      = Keywords.Any() ? [..Keywords] : null,
                Categories    = Categories.Any() ? [..Categories] : null,
                Subcategories = Subcategories.Any() ? [..Subcategories] : null,
                Systems       = Systems.Any() ? [..Systems] : null,
                Languages     = Languages.Any() ? [..Languages.Select(static l => l.Value)] : null,
                Architectures = Architectures.Any() ? [..Architectures] : null,
                Barcodes      = Barcodes.Any() ? [..Barcodes.Select(static b => b.ToModel())] : null,
                Magazines     = Magazines.Any() ? [..Magazines.Select(static m => m.ToModel())] : null,
                Books         = Books.Any() ? [..Books.Select(static b => b.ToModel())] : null,
                RequiredOperatingSystems =
                    RequiredOperatingSystems.Any()
                        ? [..RequiredOperatingSystems.Select(static os => os.ToModel())]
                        : null,
                UserManuals    = UserManuals.Any() ? [..UserManuals.Select(static um => um.ToModel())] : null,
                OpticalDiscs   = OpticalDiscs.Any() ? [..OpticalDiscs.Select(static od => od.ToModel())] : null,
                Advertisements = Advertisements.Any() ? [..Advertisements.Select(static a => a.ToModel())] : null,
                LinearMedias   = LinearMedias.Any() ? [..LinearMedias.Select(static lm => lm.ToModel())] : null,
                PciCards       = PciCards.Any() ? [..PciCards.Select(static p => p.ToModel())] : null,
                BlockMedias    = BlockMedias.Any() ? [..BlockMedias.Select(static bm => bm.ToModel())] : null,
                AudioMedias    = AudioMedias.Any() ? [..AudioMedias.Select(static am => am.ToModel())] : null
            };

            var metadataJson = new MetadataJson
            {
                AaruMetadata = metadata
            };

            string savePath = FilePath;

            if(string.IsNullOrEmpty(savePath))
            {
                var    lifetime   = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                Window mainWindow = lifetime?.MainWindow;

                if(mainWindow == null) return;

                IStorageFile file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = GUI.Dialog_Save_Metadata_File,
                    FileTypeChoices =
                    [
                        new FilePickerFileType(GUI.FileType_JSON)
                        {
                            Patterns = ["*.json"]
                        }
                    ],
                    DefaultExtension = "json"
                });

                if(file == null) return;

                savePath = file.Path.LocalPath;
                FilePath = savePath;
            }

            string json = JsonSerializer.Serialize(metadataJson, MetadataJsonContext.Default.MetadataJson);
            await File.WriteAllTextAsync(savePath, json);

            await MessageBoxManager.GetMessageBoxStandard(GUI.Title_Success,
                                                          GUI.Message_Metadata_saved_successfully,
                                                          ButtonEnum.Ok,
                                                          Icon.Success)
                                   .ShowAsync();

            _view.Close();
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            await MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                          string.Format(GUI.Error_Saving_metadata, ex.Message),
                                                          ButtonEnum.Ok,
                                                          Icon.Error)
                                   .ShowAsync();
        }
    }

    [RelayCommand]
    void Cancel()
    {
        _view.Close();
    }

    // Commands for adding items to simple string lists
    [RelayCommand]
    void AddDeveloper() => Developers.Add(string.Empty);

    [RelayCommand]
    void RemoveDeveloper(string item) => Developers.Remove(item);

    [RelayCommand]
    void AddPublisher() => Publishers.Add(string.Empty);

    [RelayCommand]
    void RemovePublisher(string item) => Publishers.Remove(item);

    [RelayCommand]
    void AddAuthor() => Authors.Add(string.Empty);

    [RelayCommand]
    void RemoveAuthor(string item) => Authors.Remove(item);

    [RelayCommand]
    void AddPerformer() => Performers.Add(string.Empty);

    [RelayCommand]
    void RemovePerformer(string item) => Performers.Remove(item);

    [RelayCommand]
    void AddKeyword() => Keywords.Add(string.Empty);

    [RelayCommand]
    void RemoveKeyword(string item) => Keywords.Remove(item);

    [RelayCommand]
    void AddCategory() => Categories.Add(string.Empty);

    [RelayCommand]
    void RemoveCategory(string item) => Categories.Remove(item);

    [RelayCommand]
    void AddSubcategory() => Subcategories.Add(string.Empty);

    [RelayCommand]
    void RemoveSubcategory(string item) => Subcategories.Remove(item);

    [RelayCommand]
    void AddSystem() => Systems.Add(string.Empty);

    [RelayCommand]
    void RemoveSystem(string item) => Systems.Remove(item);

    // Commands for adding items to enum lists
    [RelayCommand]
    void AddLanguage(object parameter)
    {
        if(parameter is LocalizedEnumValue<Language> langValue)
        {
            if(!Languages.Any(l => l.Value == langValue.Value))
                Languages.Add(langValue);
        }
    }

    [RelayCommand]
    void RemoveLanguage(LocalizedEnumValue<Language> language) => Languages.Remove(language);

    [RelayCommand]
    void AddArchitecture(Architecture architecture)
    {
        if(!Architectures.Contains(architecture)) Architectures.Add(architecture);
    }

    [RelayCommand]
    void RemoveArchitecture(Architecture architecture) => Architectures.Remove(architecture);

    // Commands for complex objects
    [RelayCommand]
    void AddBarcode() => Barcodes.Add(new BarcodeViewModel());

    [RelayCommand]
    void RemoveBarcode(BarcodeViewModel item) => Barcodes.Remove(item);

    [RelayCommand]
    void AddMagazine() => Magazines.Add(new MagazineViewModel());

    [RelayCommand]
    void RemoveMagazine(MagazineViewModel item) => Magazines.Remove(item);

    [RelayCommand]
    void AddBook() => Books.Add(new BookViewModel());

    [RelayCommand]
    void RemoveBook(BookViewModel item) => Books.Remove(item);

    [RelayCommand]
    void AddRequiredOperatingSystem() => RequiredOperatingSystems.Add(new RequiredOperatingSystemViewModel());

    [RelayCommand]
    void RemoveRequiredOperatingSystem(RequiredOperatingSystemViewModel item) => RequiredOperatingSystems.Remove(item);

    [RelayCommand]
    void AddUserManual() => UserManuals.Add(new UserManualViewModel());

    [RelayCommand]
    void RemoveUserManual(UserManualViewModel item) => UserManuals.Remove(item);

    [RelayCommand]
    void AddOpticalDisc() => OpticalDiscs.Add(new OpticalDiscViewModel());

    [RelayCommand]
    void RemoveOpticalDisc(OpticalDiscViewModel item) => OpticalDiscs.Remove(item);

    [RelayCommand]
    void AddAdvertisement() => Advertisements.Add(new AdvertisementViewModel());

    [RelayCommand]
    void RemoveAdvertisement(AdvertisementViewModel item) => Advertisements.Remove(item);

    [RelayCommand]
    void AddLinearMedia() => LinearMedias.Add(new LinearMediaViewModel());

    [RelayCommand]
    void RemoveLinearMedia(LinearMediaViewModel item) => LinearMedias.Remove(item);

    [RelayCommand]
    void AddPciCard() => PciCards.Add(new PciViewModel());

    [RelayCommand]
    void RemovePciCard(PciViewModel item) => PciCards.Remove(item);

    [RelayCommand]
    void AddBlockMedia() => BlockMedias.Add(new BlockMediaViewModel());

    [RelayCommand]
    void RemoveBlockMedia(BlockMediaViewModel item) => BlockMedias.Remove(item);

    [RelayCommand]
    void AddAudioMedia() => AudioMedias.Add(new AudioMediaViewModel());

    [RelayCommand]
    void RemoveAudioMedia(AudioMediaViewModel item) => AudioMedias.Remove(item);
}

// Helper ViewModels for complex objects
public sealed partial class BarcodeViewModel : ObservableObject
{
    [ObservableProperty]
    BarcodeType _type;

    [ObservableProperty]
    string _value;

    public BarcodeViewModel() {}

    public BarcodeViewModel([NotNull] Barcode barcode)
    {
        Type  = barcode.Type;
        Value = barcode.Value;
    }

    [NotNull]
    public Barcode ToModel() => new()
    {
        Type  = Type,
        Value = Value
    };
}

public sealed partial class MagazineViewModel : ObservableObject
{
    [ObservableProperty]
    string _editorial;
    [ObservableProperty]
    string _name;

    [ObservableProperty]
    uint? _number;

    [ObservableProperty]
    uint? _pages;

    [ObservableProperty]
    string _pageSize;

    [ObservableProperty]
    DateTime? _publicationDate;

    public MagazineViewModel() {}

    public MagazineViewModel([NotNull] Magazine magazine)
    {
        Name            = magazine.Name;
        Editorial       = magazine.Editorial;
        PublicationDate = magazine.PublicationDate;
        Number          = magazine.Number;
        Pages           = magazine.Pages;
        PageSize        = magazine.PageSize;
    }

    [NotNull]
    public Magazine ToModel() => new()
    {
        Name            = Name,
        Editorial       = Editorial,
        PublicationDate = PublicationDate,
        Number          = Number,
        Pages           = Pages,
        PageSize        = PageSize
    };
}

public sealed partial class BookViewModel : ObservableObject
{
    [ObservableProperty]
    string _author;

    [ObservableProperty]
    string _editorial;

    [ObservableProperty]
    string _name;

    [ObservableProperty]
    uint? _pages;

    [ObservableProperty]
    string _pageSize;


    [ObservableProperty]
    DateTime? _publicationDate;

    public BookViewModel() {}

    public BookViewModel([NotNull] Book book)
    {
        Name            = book.Name;
        Editorial       = book.Editorial;
        Author          = book.Author;
        PublicationDate = book.PublicationDate;
        Pages           = book.Pages;
        PageSize        = book.PageSize;
    }

    [NotNull]
    public Book ToModel() => new()
    {
        Name            = Name,
        Editorial       = Editorial,
        Author          = Author,
        PublicationDate = PublicationDate,
        Pages           = Pages,
        PageSize        = PageSize
    };
}

public sealed partial class RequiredOperatingSystemViewModel : ObservableObject
{
    [ObservableProperty]
    string _name;

    [ObservableProperty]
    ObservableCollection<string> _versions = [];

    public RequiredOperatingSystemViewModel() {}

    public RequiredOperatingSystemViewModel([NotNull] RequiredOperatingSystem os)
    {
        Name = os.Name;

        if(os.Versions == null) return;

        foreach(string version in os.Versions) Versions.Add(version);
    }

    [NotNull]
    public RequiredOperatingSystem ToModel() => new()
    {
        Name     = Name,
        Versions = Versions.Any() ? [..Versions] : null
    };
}

public sealed partial class UserManualViewModel : ObservableObject
{
    [ObservableProperty]
    uint _pages;

    [ObservableProperty]
    string _pageSize;

    public UserManualViewModel() {}

    public UserManualViewModel([NotNull] UserManual manual)
    {
        Pages    = manual.Pages;
        PageSize = manual.PageSize;
    }

    [NotNull]
    public UserManual ToModel() => new()
    {
        Pages    = Pages,
        PageSize = PageSize
    };
}

// Simplified ViewModels for complex media types (can be expanded as needed)
public sealed partial class OpticalDiscViewModel : ObservableObject
{
    [ObservableProperty]
    string _discSubType;

    [ObservableProperty]
    string _discType;
    [ObservableProperty]
    string _partNumber;

    [ObservableProperty]
    string _serialNumber;

    public OpticalDiscViewModel() {}

    public OpticalDiscViewModel([NotNull] OpticalDisc disc)
    {
        PartNumber   = disc.PartNumber;
        SerialNumber = disc.SerialNumber;
        DiscType     = disc.DiscType;
        DiscSubType  = disc.DiscSubType;
    }

    [NotNull]
    public OpticalDisc ToModel() => new()
    {
        PartNumber   = PartNumber,
        SerialNumber = SerialNumber,
        DiscType     = DiscType,
        DiscSubType  = DiscSubType
    };
}

public sealed partial class AdvertisementViewModel : ObservableObject
{
    [ObservableProperty]
    string _manufacturer;

    [ObservableProperty]
    string _product;

    public AdvertisementViewModel() {}

    public AdvertisementViewModel([NotNull] Advertisement ad)
    {
        Manufacturer = ad.Manufacturer;
        Product      = ad.Product;
    }

    [NotNull]
    public Advertisement ToModel() => new()
    {
        Manufacturer = Manufacturer,
        Product      = Product
    };
}

public sealed partial class LinearMediaViewModel : ObservableObject
{
    [ObservableProperty]
    string _manufacturer;

    [ObservableProperty]
    string _model;
    [ObservableProperty]
    string _partNumber;

    [ObservableProperty]
    string _serialNumber;

    public LinearMediaViewModel() {}

    public LinearMediaViewModel([NotNull] LinearMedia media)
    {
        PartNumber   = media.PartNumber;
        SerialNumber = media.SerialNumber;
        Manufacturer = media.Manufacturer;
        Model        = media.Model;
    }

    [NotNull]
    public LinearMedia ToModel() => new()
    {
        PartNumber   = PartNumber,
        SerialNumber = SerialNumber,
        Manufacturer = Manufacturer,
        Model        = Model
    };
}

public sealed partial class PciViewModel : ObservableObject
{
    [ObservableProperty]
    ushort _deviceID;
    [ObservableProperty]
    ushort _vendorID;

    public PciViewModel() {}

    public PciViewModel([NotNull] Pci pci)
    {
        VendorID = pci.VendorID;
        DeviceID = pci.DeviceID;
    }

    [NotNull]
    public Pci ToModel() => new()
    {
        VendorID = VendorID,
        DeviceID = DeviceID
    };
}

public sealed partial class BlockMediaViewModel : ObservableObject
{
    [ObservableProperty]
    string _firmware;
    [ObservableProperty]
    string _manufacturer;

    [ObservableProperty]
    string _model;

    [ObservableProperty]
    string _serial;

    public BlockMediaViewModel() {}

    public BlockMediaViewModel([NotNull] BlockMedia media)
    {
        Manufacturer = media.Manufacturer;
        Model        = media.Model;
        Serial       = media.Serial;
        Firmware     = media.Firmware;
    }

    [NotNull]
    public BlockMedia ToModel() => new()
    {
        Manufacturer = Manufacturer,
        Model        = Model,
        Serial       = Serial,
        Firmware     = Firmware
    };
}

public sealed partial class AudioMediaViewModel : ObservableObject
{
    [ObservableProperty]
    string _manufacturer;

    [ObservableProperty]
    string _model;

    [ObservableProperty]
    string _partNumber;

    [ObservableProperty]
    string _serialNumber;

    public AudioMediaViewModel() {}

    public AudioMediaViewModel([NotNull] AudioMedia media)
    {
        Manufacturer = media.Manufacturer;
        Model        = media.Model;
        PartNumber   = media.PartNumber;
        SerialNumber = media.SerialNumber;
    }

    [NotNull]
    public AudioMedia ToModel() => new()
    {
        Manufacturer = Manufacturer,
        Model        = Model,
        PartNumber   = PartNumber,
        SerialNumber = SerialNumber
    };
}