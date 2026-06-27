using PztGenerator;

var tests = new (string Name, Action Run)[]
{
    ("PBC subtracts buildings and hardened areas", BalanceCalculatesBioArea),
    ("Building coverage is based on site area", BalanceCalculatesBuildingCoverage),
    ("Intensity uses gross floor area", BalanceCalculatesIntensity),
    ("MPZP validation reports min and max failures", MpzpValidationReportsFailures)
};

int failedCount = 0;

foreach ((string name, Action run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failedCount++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

if (failedCount > 0)
{
    Environment.Exit(1);
}

static UrbanReport BuildSampleReport()
{
    PztAreaItem[] items =
    [
        new(PztCategories.SiteBoundary, string.Empty, 1000, 0, 0, 0),
        new(PztCategories.Building, "Projektowana", 200, 0, 3, 3),
        new(PztCategories.AccessRoad, string.Empty, 100, 0, 0, 0),
        new(PztCategories.SemiPermeable, string.Empty, 50, 0.5, 0, 0)
    ];

    return PztBalanceService.BuildUrbanReport(items, new MpzpRequirements(0, 0, 0, 0, 0), ParkingSettings.Default);
}

static void BalanceCalculatesBioArea()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(725, report.BioAreaSquareMeters, 0.0001);
    AssertEqual(72.5, report.BioPercent, 0.0001);
}

static void BalanceCalculatesBuildingCoverage()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(200, report.BuildingFootprintSquareMeters, 0.0001);
    AssertEqual(20, report.BuildingCoveragePercent, 0.0001);
}

static void BalanceCalculatesIntensity()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(600, report.GrossFloorAreaSquareMeters, 0.0001);
    AssertEqual(0.6, report.Intensity, 0.0001);
}

static void MpzpValidationReportsFailures()
{
    List<ValidationMessage> messages = MpzpValidationService.Validate(
        new MpzpRequirements(80, 0, 15, 0.8, 0.5),
        siteArea: 1000,
        buildingFootprint: 200,
        bioArea: 725,
        grossFloorArea: 600,
        buildingCoveragePercent: 20,
        bioPercent: 72.5,
        intensity: 0.6);

    AssertContains(messages, ValidationSeverity.Error, "PBC");
    AssertContains(messages, ValidationSeverity.Error, "Pow. zabudowy max.");
    AssertContains(messages, ValidationSeverity.Error, "Intensywnosc min.");
    AssertContains(messages, ValidationSeverity.Error, "Intensywnosc max.");
}

static void AssertEqual(double expected, double actual, double tolerance)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertContains(IEnumerable<ValidationMessage> messages, ValidationSeverity severity, string text)
{
    if (!messages.Any(message => message.Severity == severity && message.Text.Contains(text, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException($"Expected {severity} message containing `{text}`.");
    }
}
