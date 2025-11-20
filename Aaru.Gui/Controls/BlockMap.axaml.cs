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
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

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
    readonly Image _image;

    WriteableBitmap _bitmap;
    int             _blocksPerRow;
    int             _rows;

    uint                                                          _scanBlockSize = 1;
    ObservableCollection<(ulong startingSector, double duration)> _sectorData;

    public BlockMap()
    {
        InitializeComponent();
        _image              =  this.FindControl<Image>("BlockImage");
        _image.PointerMoved += OnPointerMoved;
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

    private void CalculateBlocksPerRow()
    {
        if(Bounds.Width <= 0) return;
        int blockWithSpacing = BlockSize + BlockSpacing;
        _blocksPerRow = Math.Max(1, (int)Bounds.Width / blockWithSpacing);
        _rows         = _sectorData == null ? 0 : (_sectorData.Count + _blocksPerRow - 1) / _blocksPerRow;
        EnsureBitmap();
    }

    void EnsureBitmap()
    {
        if(_blocksPerRow <= 0 || _rows <= 0) return;
        int blockWithSpacing = BlockSize + BlockSpacing;
        int width            = _blocksPerRow * blockWithSpacing;
        int height           = _rows         * blockWithSpacing;

        if(_bitmap == null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap?.Dispose();

            _bitmap = new WriteableBitmap(new PixelSize(width, height),
                                          new Vector(96, 96),
                                          PixelFormat.Bgra8888,
                                          AlphaFormat.Premul);

            if(_image != null)
            {
                _image.Source = _bitmap;
                _image.Width  = width;
                _image.Height = height;
            }
        }
    }

    private void RedrawAll()
    {
        if(_sectorData == null || _sectorData.Count == 0 || _blocksPerRow == 0) return;
        CalculateBlocksPerRow();

        if(_bitmap == null) return;

        using(ILockedFramebuffer fb = _bitmap.Lock())
        {
            unsafe
            {
                var data = new Span<byte>((void*)fb.Address, fb.Size.Height * fb.RowBytes);
                data.Clear();

                int blockWithSpacing = BlockSize + BlockSpacing;

                for(var i = 0; i < _sectorData.Count; i++)
                {
                    (ulong startSector, double duration) = _sectorData[i];
                    Color color = GetColorForDuration(duration);

                    int row = i       / _blocksPerRow;
                    int col = i       % _blocksPerRow;
                    DrawBlock(fb, col * blockWithSpacing, row * blockWithSpacing, color);
                }
            }
        }

        _image.InvalidateVisual();
    }

    void DrawBlock(ILockedFramebuffer fb, int x, int y, Color color)
    {
        int stride = fb.RowBytes;

        unsafe
        {
            var basePtr = (byte*)fb.Address;

            for(var dy = 0; dy < BlockSize; dy++)
            {
                byte* rowPtr = basePtr + (y + dy) * stride + x * 4;

                for(var dx = 0; dx < BlockSize; dx++)
                {
                    rowPtr[0] =  color.B;
                    rowPtr[1] =  color.G;
                    rowPtr[2] =  color.R;
                    rowPtr[3] =  255;
                    rowPtr    += 4;
                }
            }
        }
    }

    private void OnSectorDataChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(_sectorData == null) return;

        if(e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            // If not laid out yet, defer to full redraw
            if(_blocksPerRow == 0)
            {
                RedrawAll();

                return;
            }

            // Check if rows changed (only depends on count now)
            int newRows = (_sectorData.Count + _blocksPerRow - 1) / _blocksPerRow;

            if(newRows != _rows)
            {
                _rows = newRows;
                EnsureBitmap();
                RedrawAll();

                return;
            }

            if(_bitmap == null) EnsureBitmap();

            if(_bitmap == null) return;

            using(ILockedFramebuffer fb = _bitmap.Lock())
            {
                int blockWithSpacing = BlockSize + BlockSpacing;
                int startIndex       = e.NewStartingIndex;
                int count            = e.NewItems.Count;

                for(var i = 0; i < count; i++)
                {
                    int dataIndex = startIndex + i;

                    if(dataIndex >= _sectorData.Count) break;
                    (ulong startSector, double duration) = _sectorData[dataIndex];
                    Color color = GetColorForDuration(duration);
                    int   row   = dataIndex / _blocksPerRow;
                    int   col   = dataIndex % _blocksPerRow;
                    DrawBlock(fb, col       * blockWithSpacing, row * blockWithSpacing, color);
                }
            }

            _image.InvalidateVisual();
        }
        else
            RedrawAll();
    }

    void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if(_sectorData == null || _sectorData.Count == 0 || _blocksPerRow == 0)
        {
            ToolTip.SetTip(_image, null);

            return;
        }

        Point p                = e.GetPosition(_image);
        int   blockWithSpacing = BlockSize + BlockSpacing;

        if(p.X < 0 || p.Y < 0)
        {
            ToolTip.SetTip(_image, null);

            return;
        }

        var col = (int)(p.X / blockWithSpacing);
        var row = (int)(p.Y / blockWithSpacing);

        if(col < 0 || row < 0)
        {
            ToolTip.SetTip(_image, null);

            return;
        }

        int index = row * _blocksPerRow + col;

        if(index < 0 || index >= _sectorData.Count)
        {
            ToolTip.SetTip(_image, null);

            return;
        }

        (ulong startSector, double duration) = _sectorData[index];
        ulong endSector = startSector + (_scanBlockSize > 0 ? _scanBlockSize - 1u : 0u);

        string tooltipText = _scanBlockSize > 1
                                 ? $"Sectors {startSector} - {endSector}\nBlock size: {_scanBlockSize} sectors\nDuration: {duration:F2} ms"
                                 : $"Sector {startSector}\nDuration: {duration:F2} ms";

        ToolTip.SetTip(_image, tooltipText);
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