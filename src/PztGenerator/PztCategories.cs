namespace PztGenerator;

public static class PztCategories
{
    public const string SiteBoundary = "Granica terenu";
    public const string Building = "Zabudowa";
    public const string AccessRoad = "Dojazdy";
    public const string Walkway = "Dojscia";
    public const string Parking = "Parking";
    public const string BioActive = "Biologicznie czynna";
    public const string SemiPermeable = "Czesciowo biologicznie czynna";
    public const string Unassigned = "Nieprzypisane";
    public const string Invalid = "Nieprzypisane / bledne";

    public static IReadOnlySet<string> Known { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SiteBoundary,
        Building,
        AccessRoad,
        Walkway,
        Parking,
        BioActive,
        SemiPermeable
    };

    public static bool IsKnown(string category)
    {
        return Known.Contains(category);
    }

    public static bool IsHardened(string category)
    {
        return string.Equals(category, AccessRoad, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(category, Walkway, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(category, Parking, StringComparison.OrdinalIgnoreCase);
    }
}
