// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HexViewPanel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI custom controls.
//
// --[ Description ] ----------------------------------------------------------
//
//     A hex view control that displays data in three synchronized columns:
//     offset, hexadecimal bytes, and ASCII representation.
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
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Aaru.Gui.Controls;

/// <summary>Represents a color range for highlighting bytes in the hex view.</summary>
public class ColorRange
{
    /// <summary>Gets or sets the starting byte index (inclusive).</summary>
    public int Start { get; set; }

    /// <summary>Gets or sets the ending byte index (inclusive).</summary>
    public int End { get; set; }

    /// <summary>Gets or sets the color to apply to this range.</summary>
    public IBrush Color { get; set; }
}

/// <summary>Hex view control with synchronized scrolling of offset, hex, and ASCII columns.</summary>
public class HexViewPanel : UserControl
{
    private const int BYTES_PER_LINE = 16;

    public static readonly StyledProperty<byte[]> DataProperty =
        AvaloniaProperty.Register<HexViewPanel, byte[]>(nameof(Data));

    public static readonly StyledProperty<string> OffsetHeaderProperty =
        AvaloniaProperty.Register<HexViewPanel, string>(nameof(OffsetHeader), "Offset");

    public static readonly StyledProperty<string> AsciiHeaderProperty =
        AvaloniaProperty.Register<HexViewPanel, string>(nameof(AsciiHeader), "ASCII");

    public static readonly StyledProperty<List<ColorRange>> ColorRangesProperty =
        AvaloniaProperty.Register<HexViewPanel, List<ColorRange>>(nameof(ColorRanges));

    private readonly TextBlock    _asciiContent;
    private readonly TextBlock    _asciiHeaderText;
    private readonly TextBlock    _hexContent;
    private readonly TextBlock    _offsetContent;
    private readonly TextBlock    _offsetHeaderText;
    private readonly ScrollViewer _scrollViewer;
    private          byte[]       _data = [];

    static HexViewPanel()
    {
        DataProperty.Changed.AddClassHandler<HexViewPanel>(static (sender, _) => sender.OnDataChanged());

        OffsetHeaderProperty.Changed.AddClassHandler<HexViewPanel>(static (sender, _) =>
                                                                       sender.OnOffsetHeaderChanged());

        AsciiHeaderProperty.Changed.AddClassHandler<HexViewPanel>(static (sender, _) => sender.OnAsciiHeaderChanged());

        ColorRangesProperty.Changed.AddClassHandler<HexViewPanel>(static (sender, _) => sender.OnColorRangesChanged());
    }

    public HexViewPanel()
    {
        // Create header for hex column with byte positions
        var hexHeader = new StringBuilder();

        for(var i = 0; i < BYTES_PER_LINE; i++)
        {
            if(i > 0) hexHeader.Append(' ');
            hexHeader.Append(i.ToString("X2"));
        }

        // Create the three content TextBlocks
        _offsetContent = new TextBlock
        {
            FontFamily   = new FontFamily("Courier New"),
            FontSize     = 12,
            TextWrapping = TextWrapping.NoWrap,
            Foreground   = Brushes.White,
            Padding      = new Thickness(5)
        };

        _hexContent = new TextBlock
        {
            FontFamily          = new FontFamily("Courier New"),
            FontSize            = 12,
            TextWrapping        = TextWrapping.NoWrap,
            Foreground          = Brushes.White,
            Padding             = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _asciiContent = new TextBlock
        {
            FontFamily   = new FontFamily("Courier New"),
            FontSize     = 12,
            TextWrapping = TextWrapping.NoWrap,
            Foreground   = Brushes.White,
            Padding      = new Thickness(5)
        };

        // Create a shared grid that contains both headers and content with matching column definitions
        var sharedGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            RowDefinitions    = new RowDefinitions("Auto,*")
        };

        // Offset column header
        _offsetHeaderText = new TextBlock
        {
            Text                = OffsetHeader,
            FontFamily          = new FontFamily("Courier New"),
            FontSize            = 12,
            FontWeight          = FontWeight.Bold,
            Foreground          = Brushes.White,
            Padding             = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var offsetHeader = new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#2D2D30")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Child           = _offsetHeaderText
        };

        Grid.SetColumn(offsetHeader, 0);
        Grid.SetRow(offsetHeader, 0);
        sharedGrid.Children.Add(offsetHeader);

        // Hex column header
        var hexHeaderBlock = new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#2D2D30")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Child = new TextBlock
            {
                Text                = hexHeader.ToString(),
                FontFamily          = new FontFamily("Courier New"),
                FontSize            = 12,
                FontWeight          = FontWeight.Bold,
                Foreground          = Brushes.White,
                Padding             = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        Grid.SetColumn(hexHeaderBlock, 1);
        Grid.SetRow(hexHeaderBlock, 0);
        sharedGrid.Children.Add(hexHeaderBlock);

        // ASCII column header
        _asciiHeaderText = new TextBlock
        {
            Text                = AsciiHeader,
            FontFamily          = new FontFamily("Courier New"),
            FontSize            = 12,
            FontWeight          = FontWeight.Bold,
            Foreground          = Brushes.White,
            Padding             = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var asciiHeader = new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#2D2D30")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child           = _asciiHeaderText
        };

        Grid.SetColumn(asciiHeader, 2);
        Grid.SetRow(asciiHeader, 0);
        sharedGrid.Children.Add(asciiHeader);

        // Offset content column (directly in the shared grid)
        var offsetBorder = new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#1E1E1E")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child           = _offsetContent
        };

        Grid.SetColumn(offsetBorder, 0);
        Grid.SetRow(offsetBorder, 1);
        sharedGrid.Children.Add(offsetBorder);

        // Hex content column (directly in the shared grid)
        var hexBorder = new Border
        {
            Background      = Brushes.Black,
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child           = _hexContent
        };

        Grid.SetColumn(hexBorder, 1);
        Grid.SetRow(hexBorder, 1);
        sharedGrid.Children.Add(hexBorder);

        // ASCII content column (directly in the shared grid)
        var asciiBorder = new Border
        {
            Background = Brushes.Black,
            Child      = _asciiContent
        };

        Grid.SetColumn(asciiBorder, 2);
        Grid.SetRow(asciiBorder, 1);
        sharedGrid.Children.Add(asciiBorder);

        // ScrollViewer that wraps the entire shared grid
        _scrollViewer = new ScrollViewer
        {
            Content                       = sharedGrid,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Main border
        var mainBorder = new Border
        {
            Child           = _scrollViewer,
            BorderBrush     = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(1),
            Background      = Brushes.Black
        };

        Content = mainBorder;
    }

    public byte[] Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public string OffsetHeader
    {
        get => GetValue(OffsetHeaderProperty);
        set => SetValue(OffsetHeaderProperty, value);
    }

    public string AsciiHeader
    {
        get => GetValue(AsciiHeaderProperty);
        set => SetValue(AsciiHeaderProperty, value);
    }

    public List<ColorRange> ColorRanges
    {
        get => GetValue(ColorRangesProperty);
        set => SetValue(ColorRangesProperty, value);
    }

    private void OnDataChanged()
    {
        _data = Data ?? [];
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if(_data.Length == 0)
        {
            _offsetContent.Text = string.Empty;
            _hexContent.Inlines?.Clear();
            _asciiContent.Inlines?.Clear();

            return;
        }

        var offsetSb   = new StringBuilder();
        int totalLines = (_data.Length + BYTES_PER_LINE - 1) / BYTES_PER_LINE;

        // Clear existing inlines
        _hexContent.Inlines?.Clear();
        _asciiContent.Inlines?.Clear();

        // Build color lookup for quick access (byte index -> color)
        var colorLookup = new Dictionary<int, IBrush>();

        if(ColorRanges != null)
        {
            foreach(ColorRange range in ColorRanges)
            {
                for(int i = range.Start; i <= range.End && i < _data.Length; i++)
                {
                    if(!colorLookup.ContainsKey(i)) colorLookup[i] = range.Color;
                }
            }
        }

        for(var lineIndex = 0; lineIndex < totalLines; lineIndex++)
        {
            int offset = lineIndex * BYTES_PER_LINE;

            if(offset >= _data.Length) break;

            // Offset column (simple text)
            offsetSb.AppendLine(offset.ToString("X8"));

            // Hex column - build runs based on color ranges
            int endIdx = Math.Min(offset + BYTES_PER_LINE, _data.Length);
            BuildHexRuns(offset, endIdx, colorLookup);

            // Add line break for hex
            if(lineIndex < totalLines - 1) _hexContent.Inlines?.Add(new Run("\n"));

            // ASCII column - build runs based on color ranges
            BuildAsciiRuns(offset, endIdx, colorLookup);

            // Add line break for ASCII
            if(lineIndex < totalLines - 1) _asciiContent.Inlines?.Add(new Run("\n"));
        }

        _offsetContent.Text = offsetSb.ToString();
    }

    private void BuildHexRuns(int offset, int endIdx, Dictionary<int, IBrush> colorLookup)
    {
        var    currentRun   = new StringBuilder();
        IBrush currentColor = null;

        for(int i = offset; i < endIdx; i++)
        {
            // Get the color for this byte
            IBrush byteColor = colorLookup.TryGetValue(i, out IBrush value) ? value : Brushes.White;

            // If this is the first byte or color changed, flush previous run
            if(currentColor == null)
                currentColor = byteColor;
            else if(!Equals(byteColor, currentColor) && currentRun.Length > 0)
            {
                // Flush current run
                _hexContent.Inlines?.Add(new Run(currentRun.ToString())
                {
                    Foreground = currentColor
                });

                currentRun.Clear();
                currentColor = byteColor;
            }

            if(i > offset) currentRun.Append(' ');

            currentRun.Append(_data[i].ToString("X2"));
        }

        // Flush remaining run
        if(currentRun.Length > 0 && currentColor != null)
        {
            _hexContent.Inlines?.Add(new Run(currentRun.ToString())
            {
                Foreground = currentColor
            });
        }

        // Padding for hex column to maintain alignment
        int bytesWritten = endIdx - offset;

        if(bytesWritten >= BYTES_PER_LINE) return;

        var padding = new StringBuilder();
        for(int i = bytesWritten; i < BYTES_PER_LINE; i++) padding.Append("   ");

        _hexContent.Inlines?.Add(new Run(padding.ToString())
        {
            Foreground = Brushes.White
        });
    }

    private void BuildAsciiRuns(int offset, int endIdx, Dictionary<int, IBrush> colorLookup)
    {
        var    currentRun   = new StringBuilder();
        IBrush currentColor = null;

        for(int i = offset; i < endIdx; i++)
        {
            // Get the color for this byte
            IBrush byteColor = colorLookup.ContainsKey(i) ? colorLookup[i] : Brushes.White;

            // If this is the first byte or color changed, flush previous run
            if(currentColor == null)
                currentColor = byteColor;
            else if(!Equals(byteColor, currentColor) && currentRun.Length > 0)
            {
                // Flush current run
                _asciiContent.Inlines?.Add(new Run(currentRun.ToString())
                {
                    Foreground = currentColor
                });

                currentRun.Clear();
                currentColor = byteColor;
            }

            byte c = _data[i];

            if(c is >= 32 and <= 126)
                currentRun.Append((char)c);
            else
                currentRun.Append('.');
        }

        // Flush remaining run
        if(currentRun.Length > 0 && currentColor != null)
        {
            _asciiContent.Inlines?.Add(new Run(currentRun.ToString())
            {
                Foreground = currentColor
            });
        }
    }

    private void OnColorRangesChanged()
    {
        UpdateDisplay();
    }

    private void OnOffsetHeaderChanged()
    {
        _offsetHeaderText?.Text = OffsetHeader;
    }

    private void OnAsciiHeaderChanged()
    {
        _asciiHeaderText?.Text = AsciiHeader;
    }
}