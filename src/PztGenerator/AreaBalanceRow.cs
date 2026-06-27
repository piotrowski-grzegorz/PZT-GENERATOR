namespace PztGenerator;

public sealed record AreaBalanceRow(
    string Category,
    string Status,
    int AreaCount,
    double AreaSquareMeters,
    double GrossFloorAreaSquareMeters,
    double BioAreaSquareMeters,
    string BioFactorLabel);
