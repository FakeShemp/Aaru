// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BlockMap.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI custom controls.
//
// --[ Description ] ----------------------------------------------------------
//
//     A block map control to visualize sector access times.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Aaru.Gui.Controls;

public partial class BlockMap : UserControl
{
    const int    BlockSize    = 4;     // Size of each block in pixels
    const int    BlockSpacing = 1;     // Spacing between blocks
    const double MinDuration  = 1.0;   // Green threshold (ms)
    const double MaxDuration  = 500.0; // Red threshold (ms)

    public static readonly StyledProperty<ObservableCollection<(ulong startingSector, double duration)>>
        SectorDataProperty =
            AvaloniaProperty
               .Register<BlockMap, ObservableCollection<(ulong startingSector, double duration)>>(nameof(SectorData));

    public static readonly StyledProperty<uint> ScanBlockSizeProperty =
        AvaloniaProperty.Register<BlockMap, uint>(nameof(ScanBlockSize), 1u);
    int _blocksPerRow;

    readonly Canvas                                               _canvas;
    uint                                                          _scanBlockSize = 1;
    ObservableCollection<(ulong startingSector, double duration)> _sectorData;
    int                                                           _totalBlocksDrawn;

    public BlockMap()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("BlockCanvas");
    }

    public ObservableCollection<(ulong startingSector, double duration)> SectorData
    {
        get => GetValue(SectorDataProperty);
        set => SetValue(SectorDataProperty, value);
    }

    public uint ScanBlockSize
    {
        get => GetValue(ScanBlockSizeProperty);
        set => SetValue(ScanBlockSizeProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == SectorDataProperty)
        {
            if(_sectorData != null) _sectorData.CollectionChanged -= OnSectorDataChanged;

            _sectorData = change.GetNewValue<ObservableCollection<(ulong startingSector, double duration)>>();

            if(_sectorData != null)
            {
                _sectorData.CollectionChanged += OnSectorDataChanged;
                RedrawAll();
            }
        }
        else if(change.Property == ScanBlockSizeProperty)
        {
            _scanBlockSize = change.GetNewValue<uint>();
            RedrawAll();
        }
        else if(change.Property == BoundsProperty)
        {
            CalculateBlocksPerRow();
            RedrawAll();
        }
    }

    private void OnSectorDataChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Incremental draw for added items
            DrawNewBlocks(e.NewStartingIndex, e.NewItems.Count);
        }
        else
        {
            // Full redraw for other operations
            RedrawAll();
        }
    }

    private void CalculateBlocksPerRow()
    {
        if(Bounds.Width <= 0) return;

        var availableWidth   = (int)Bounds.Width;
        int blockWithSpacing = BlockSize + BlockSpacing;
        _blocksPerRow = Math.Max(1, availableWidth / blockWithSpacing);
    }

    private void RedrawAll()
    {
        if(_canvas == null || _sectorData == null || _sectorData.Count == 0) return;

        _canvas.Children.Clear();
        CalculateBlocksPerRow();
        _totalBlocksDrawn = 0;

        DrawNewBlocks(0, _sectorData.Count);
    }

    private void DrawNewBlocks(int startIndex, int count)
    {
        if(_canvas == null || _sectorData == null || _blocksPerRow == 0) return;

        int blockWithSpacing = BlockSize + BlockSpacing;

        for(int i = startIndex; i < startIndex + count && i < _sectorData.Count; i++)
        {
            (ulong startingSector, double duration) = _sectorData[i];
            Color color = GetColorForDuration(duration);

            // Calculate position in grid
            int blockIndex = _totalBlocksDrawn;
            int row        = blockIndex / _blocksPerRow;
            int col        = blockIndex % _blocksPerRow;

            // Create and position rectangle
            var rect = new Border
            {
                Width           = BlockSize,
                Height          = BlockSize,
                Background      = new SolidColorBrush(color),
                BorderBrush     = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            Canvas.SetLeft(rect, col * blockWithSpacing);
            Canvas.SetTop(rect, row  * blockWithSpacing);

            _canvas.Children.Add(rect);
            _totalBlocksDrawn++;
        }

        // Update canvas height based on rows needed
        int totalRows = (_totalBlocksDrawn + _blocksPerRow - 1) / _blocksPerRow;
        _canvas.Height = totalRows * blockWithSpacing;
    }

    private Color GetColorForDuration(double duration)
    {
        // Clamp duration between min and max
        double clampedDuration = Math.Max(MinDuration, Math.Min(MaxDuration, duration));

        // Calculate normalized position (0 = green/fast, 1 = red/slow)
        double normalized = (clampedDuration - MinDuration) / (MaxDuration - MinDuration);

        // Interpolate through color spectrum with more gradients:
        // Green -> Lime -> Yellow -> Orange -> Red-Orange -> Dark Red
        if(normalized <= 0.17) // Green to Lime
        {
            double t = normalized / 0.17;

            return Color.FromRgb((byte)(0 + t * 128), // R: 0 -> 128
                                 255,                 // G: stays 255
                                 0                    // B: stays 0
                                );
        }

        if(normalized <= 0.34) // Lime to Yellow
        {
            double t = (normalized - 0.17) / 0.17;

            return Color.FromRgb((byte)(128 + t * 127), // R: 128 -> 255
                                 255,                   // G: stays 255
                                 0                      // B: stays 0
                                );
        }

        if(normalized <= 0.50) // Yellow to Orange
        {
            double t = (normalized - 0.34) / 0.16;

            return Color.FromRgb(255,                  // R: stays 255
                                 (byte)(255 - t * 85), // G: 255 -> 170
                                 0                     // B: stays 0
                                );
        }

        if(normalized <= 0.67) // Orange to Orange-Red
        {
            double t = (normalized - 0.50) / 0.17;

            return Color.FromRgb(255,                  // R: stays 255
                                 (byte)(170 - t * 85), // G: 170 -> 85
                                 (byte)(0   + t * 64)  // B: 0 -> 64
                                );
        }

        if(normalized <= 0.84) // Orange-Red to Red
        {
            double t = (normalized - 0.67) / 0.17;

            return Color.FromRgb(255,                 // R: stays 255
                                 (byte)(85 - t * 85), // G: 85 -> 0
                                 (byte)(64 + t * 64)  // B: 64 -> 128
                                );
        }
        else // Red to Dark Red
        {
            double t = (normalized - 0.84) / 0.16;

            return Color.FromRgb((byte)(255 - t * 55), // R: 255 -> 200
                                 0,                    // G: stays 0
                                 (byte)(128 + t * 127) // B: 128 -> 255
                                );
        }
    }
}