// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Text User Interface.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the "Software"),
//     to deal in the Software without restriction, including without limitation
//     the rights to use, copy, modify, merge, publish, distribute, sublicense,
//     and/or sell copies of the Software, and to permit persons to whom the
//     Software is furnished to do so, subject to the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
//     THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
//     OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//     ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//     OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Aaru.Tui.Controls;

public partial class SpectreTextBlock : TextBlock
{
    // Matches color formats like:
    // "red on blue", "#ff0000 on blue", "rgb(255,0,0) on blue", etc.
    private static readonly Regex _colorMarkupRegex = ColorRegex();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == TextProperty) UpdateMarkup();
    }

    void UpdateMarkup()
    {
        if(string.IsNullOrEmpty(Text))
        {
            Inlines?.Clear();

            return;
        }

        var inlines = new InlineCollection();

        List<MarkupTag> markups = ParseMarkups(Text);

        if(markups.Count == 0)
        {
            inlines.Add(new Run(Text));
            Inlines = inlines;

            return;
        }

        // Create a list of tag ranges to exclude from the text
        var tagRanges = new List<(int start, int end)>();

        foreach(MarkupTag markup in markups)
        {
            // Add opening tag range
            tagRanges.Add((markup.Start, markup.OpenTagEnd));

            // Add closing tag range
            tagRanges.Add((markup.CloseTagStart, markup.End));
        }

        // Create breakpoints at all positions (start, end of tags, start and end of text)
        var breakpoints = new SortedSet<int>
        {
            0,
            Text.Length
        };

        // Add all tag boundaries as breakpoints
        foreach((int start, int end) range in tagRanges)
        {
            breakpoints.Add(range.start);
            breakpoints.Add(range.end);
        }

        var breakpointList = breakpoints.ToList();

        for(var i = 0; i < breakpointList.Count - 1; i++)
        {
            int start = breakpointList[i];
            int end   = breakpointList[i + 1];

            // Skip empty segments
            if(start == end) continue;

            // Skip this segment if it overlaps with any tag range
            bool isInsideTag = tagRanges.Any(range => start >= range.start && start < range.end  ||
                                                      end   > range.start  && end   <= range.end ||
                                                      start <= range.start && end   >= range.end);

            if(isInsideTag) continue;

            // Find which markup tags apply to this content segment
            var applicableMarkups = markups.Where(m => m.OpenTagEnd <= start && m.CloseTagStart >= end).ToList();

            var run = new Run(Text.Substring(start, end - start));

            foreach(MarkupTag markup in applicableMarkups)
            {
                // This is very simple parsing, possibly more complex legal markup would fail
                if(markup.Tag.Contains("bold")) run.FontWeight = FontWeight.Bold;

                if(markup.Tag.Contains("italic")) run.FontStyle = FontStyle.Italic;

                if(markup.Tag.Contains("underline")) run.TextDecorations = Avalonia.Media.TextDecorations.Underline;

                if(!_colorMarkupRegex.IsMatch(markup.Tag)) continue;

                Match   match      = _colorMarkupRegex.Match(markup.Tag);
                string  foreground = match.Groups["fg"].Value;
                string? background = match.Groups["bg"].Success ? match.Groups["bg"].Value : null;

                // Apply foreground color
                if(!string.IsNullOrEmpty(foreground))
                {
                    IBrush? fgBrush                    = ParseColor(foreground);
                    if(fgBrush != null) run.Foreground = fgBrush;
                }

                // Apply background color
                if(string.IsNullOrEmpty(background)) continue;

                IBrush? bgBrush = ParseColor(background);

                if(bgBrush != null) run.Background = bgBrush;
            }

            inlines.Add(run);
        }

        Inlines = inlines;
    }

    static IBrush ParseColor(string color)
    {
        try
        {
            // Handle hex colors like #ff0000
            if(color.StartsWith("#")) return new SolidColorBrush(Color.Parse(color));

            // Handle rgb(r,g,b) format
            if(color.StartsWith("rgb(") && color.EndsWith(")"))
            {
                string[] values = color.Substring(4, color.Length - 5).Split(',');

                if(values.Length == 3                          &&
                   byte.TryParse(values[0].Trim(), out byte r) &&
                   byte.TryParse(values[1].Trim(), out byte g) &&
                   byte.TryParse(values[2].Trim(), out byte b))
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
            }

            // Handle named colors like "red", "blue", etc.
            else
            {
                // Not all Spectre colors map correctly, need to do a manual mapping, but the list is huge
                return new SolidColorBrush(Color.Parse(color));
            }
        }
        catch
        {
            // If parsing fails, return null
        }

        return null;
    }

    List<MarkupTag> ParseMarkups(string text)
    {
        var result   = new List<MarkupTag>();
        var tagStack = new Stack<(int start, int openTagEnd, string tag)>();
        var i        = 0;

        while(i < text.Length)
        {
            if(text[i] == '[')
            {
                int tagStart = i;
                i++;

                // Check if it's a closing tag [/]
                if(i < text.Length && text[i] == '/')
                {
                    i++;

                    if(i < text.Length && text[i] == ']')
                    {
                        // Found [/], close the most recent tag
                        if(tagStack.Count > 0)
                        {
                            (int openStart, int openTagEnd, string tag) = tagStack.Pop();
                            int closeTagEnd = i + 1; // After the ']' of [/]
                            result.Add(new MarkupTag(openStart, closeTagEnd, tag, openTagEnd, tagStart));
                        }

                        i++;
                    }
                }
                else
                {
                    // Parse opening tag like [red], [bold], etc.
                    int tagNameStart = i;
                    while(i < text.Length && text[i] != ']' && text[i] != '[') i++;

                    if(i >= text.Length || text[i] != ']') continue;

                    string tagName = text.Substring(tagNameStart, i - tagNameStart);

                    if(!string.IsNullOrWhiteSpace(tagName))
                    {
                        int openTagEnd = i + 1; // After the ']'
                        tagStack.Push((tagStart, openTagEnd, tagName));
                    }

                    i++;
                }
            }
            else
                i++;
        }

        return result;
    }

    [GeneratedRegex(@"^(?<fg>#[0-9a-fA-F]{6}|rgb\(\d{1,3},\d{1,3},\d{1,3}\)|[a-zA-Z]+)(\s+on\s+(?<bg>#[0-9a-fA-F]{6}|rgb\(\d{1,3},\d{1,3},\d{1,3}\)|[a-zA-Z]+))?$",
                    RegexOptions.Compiled)]
    private static partial Regex ColorRegex();

#region Nested type: MarkupTag

    sealed record MarkupTag(int Start, int End, string Tag, int OpenTagEnd, int CloseTagStart);

#endregion
}