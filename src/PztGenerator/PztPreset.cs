namespace PztGenerator;

public sealed record PztPreset(
    string Name,
    string Category,
    string Status,
    double BioFactor,
    double Floors,
    double StoreyHeight)
{
    public static IReadOnlyList<PztPreset> All { get; } =
    [
        new("Granica terenu / dzialki", PztCategories.SiteBoundary, string.Empty, 0, 0, 0),
        new("Zabudowa projektowana", PztCategories.Building, "Projektowana", 0, 1, 3),
        new("Zabudowa istniejaca", PztCategories.Building, "Istniejaca", 0, 1, 3),
        new("Dojazdy", PztCategories.AccessRoad, string.Empty, 0, 0, 0),
        new("Dojscia", PztCategories.Walkway, string.Empty, 0, 0, 0),
        new("Parking", PztCategories.Parking, string.Empty, 0, 0, 0),
        new("Biologicznie czynna", PztCategories.BioActive, string.Empty, 1, 0, 0),
        new("Czesciowo biologicznie czynna 50%", PztCategories.SemiPermeable, string.Empty, 0.5, 0, 0)
    ];
}
