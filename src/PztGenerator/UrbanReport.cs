namespace PztGenerator;

public sealed record UrbanReport(
    IReadOnlyCollection<AreaBalanceRow> Rows,
    MpzpRequirements Requirements,
    double SiteAreaSquareMeters,
    double BuildingFootprintSquareMeters,
    double HardenedAreaSquareMeters,
    double GrossFloorAreaSquareMeters,
    double BioAreaSquareMeters,
    double BuildingCoveragePercent,
    double BioPercent,
    double Intensity,
    double ParkingAreaSquareMeters,
    int ParkingSpaceCount,
    int RegularParkingSpaceCount,
    int AccessibleParkingSpaceCount,
    ParkingSettings ParkingSettings,
    IReadOnlyList<ValidationMessage> ValidationMessages,
    IReadOnlyCollection<PlotBalanceReport>? PlotReports = null)
{
    public double TotalAreaSquareMeters => Rows.Sum(row => row.AreaSquareMeters);

    public IReadOnlyCollection<PlotBalanceReport> PlotReportsSafe => PlotReports ?? Array.Empty<PlotBalanceReport>();
}

public sealed record PlotBalanceReport(string PlotId, UrbanReport Report);
