namespace PztGenerator;

public sealed record PztAreaItem(
    string Category,
    string Status,
    double AreaSquareMeters,
    double BioFactor,
    double Floors,
    double StoreyHeight)
{
    public double BioAreaSquareMeters => AreaSquareMeters * BioFactor;

    public double GrossFloorAreaSquareMeters => IsBuilding ? AreaSquareMeters * Math.Max(Floors, 1) : 0;

    public bool IsSiteBoundary => string.Equals(Category, PztCategories.SiteBoundary, StringComparison.OrdinalIgnoreCase);

    public bool IsBuilding => string.Equals(Category, PztCategories.Building, StringComparison.OrdinalIgnoreCase);

    public bool IsHardened => PztCategories.IsHardened(Category);

    public bool IsUnassigned => string.Equals(Category, PztCategories.Unassigned, StringComparison.OrdinalIgnoreCase);

    public bool HasInvalidCategory => string.Equals(Category, PztCategories.Invalid, StringComparison.OrdinalIgnoreCase);
}
