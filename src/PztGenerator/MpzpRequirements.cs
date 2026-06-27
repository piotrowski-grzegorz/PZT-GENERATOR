namespace PztGenerator;

public sealed record MpzpRequirements(
    double MinBioPercent,
    double MinBuildingCoveragePercent,
    double MaxBuildingCoveragePercent,
    double MinIntensity,
    double MaxIntensity)
{
    public bool HasAnyValue =>
        MinBioPercent > 0 ||
        MinBuildingCoveragePercent > 0 ||
        MaxBuildingCoveragePercent > 0 ||
        MinIntensity > 0 ||
        MaxIntensity > 0;
}
