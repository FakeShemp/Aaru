using System.Globalization;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    private (bool success, uint cylinders, uint heads, uint sectors)? ParseGeometry(string geometryString)
    {
        // Parses CHS (Cylinder/Head/Sector) geometry string in format "C/H/S" or "C-H-S"
        // Returns tuple with success flag and parsed values, or null if not specified

        if(geometryString == null) return null;

        string[] geometryPieces = geometryString.Split('/');

        if(geometryPieces.Length == 0) geometryPieces = geometryString.Split('-');

        if(geometryPieces.Length != 3)
        {
            StoppingErrorMessage?.Invoke(UI.Invalid_geometry_specified);

            return (false, 0, 0, 0);
        }

        if(!uint.TryParse(geometryPieces[0], CultureInfo.InvariantCulture, out uint cylinders) || cylinders == 0)
        {
            StoppingErrorMessage?.Invoke(UI.Invalid_number_of_cylinders_specified);

            return (false, 0, 0, 0);
        }

        if(!uint.TryParse(geometryPieces[1], CultureInfo.InvariantCulture, out uint heads) || heads == 0)
        {
            StoppingErrorMessage?.Invoke(UI.Invalid_number_of_heads_specified);

            return (false, 0, 0, 0);
        }

        if(uint.TryParse(geometryPieces[2], CultureInfo.InvariantCulture, out uint sectors) && sectors != 0)
            return (true, cylinders, heads, sectors);

        StoppingErrorMessage?.Invoke(UI.Invalid_sectors_per_track_specified);

        return (false, 0, 0, 0);
    }
}