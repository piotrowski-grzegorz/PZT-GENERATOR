namespace PztGenerator;

public sealed record ParkingSettings(
    double RegularSpaceWidthMeters,
    double RegularSpaceLengthMeters,
    int AccessibleSpaceCount,
    double AccessibleSpaceWidthMeters,
    double AccessibleSpaceLengthMeters)
{
    public static ParkingSettings Default { get; } = new(2.5, 5.0, 0, 3.6, 6.0);

    public double RegularSpaceAreaSquareMeters => RegularSpaceWidthMeters * RegularSpaceLengthMeters;

    public double AccessibleSpaceAreaSquareMeters => AccessibleSpaceWidthMeters * AccessibleSpaceLengthMeters;
}
