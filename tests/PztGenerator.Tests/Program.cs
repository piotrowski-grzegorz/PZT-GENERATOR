using System.IO.Compression;
using System.Xml.Linq;
using PztGenerator;

var tests = new (string Name, Action Run)[]
{
    ("PBC subtracts buildings and hardened areas", BalanceCalculatesBioArea),
    ("Building coverage is based on site area", BalanceCalculatesBuildingCoverage),
    ("Intensity uses gross floor area", BalanceCalculatesIntensity),
    ("MPZP validation reports min and max failures", MpzpValidationReportsFailures),
    ("Balance exposes automatic PBC and existing buildings", BalanceExposesAutomaticPbcAndExistingBuildings),
    ("Balance creates per-plot reports", BalanceCreatesPerPlotReports),
    ("DOCX export creates styled PAB tables", DocxExportCreatesStyledPabTables)
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
        new(PztCategories.Building, "Istniejaca", 75, 0, 1, 3),
        new(PztCategories.AccessRoad, "Projektowana", 100, 0, 0, 0),
        new(PztCategories.SemiPermeable, "Projektowana", 50, 0.5, 0, 0),
        new(PztCategories.Parking, "Istniejaca", 25, 0, 0, 0)
    ];

    return PztBalanceService.BuildUrbanReport(items, new MpzpRequirements(70, 0, 25, 0.1, 0.8), ParkingSettings.Default);
}

static void BalanceCalculatesBioArea()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(625, report.BioAreaSquareMeters, 0.0001);
    AssertEqual(62.5, report.BioPercent, 0.0001);
}

static void BalanceCalculatesBuildingCoverage()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(275, report.BuildingFootprintSquareMeters, 0.0001);
    AssertEqual(27.5, report.BuildingCoveragePercent, 0.0001);
}

static void BalanceCalculatesIntensity()
{
    UrbanReport report = BuildSampleReport();

    AssertEqual(675, report.GrossFloorAreaSquareMeters, 0.0001);
    AssertEqual(0.675, report.Intensity, 0.0001);
}

static void BalanceExposesAutomaticPbcAndExistingBuildings()
{
    UrbanReport report = BuildSampleReport();

    AreaBalanceRow automaticPbc = report.Rows.FirstOrDefault(row =>
        row.Category == PztCategories.BioActive &&
        row.Status.Contains("Automatyczna", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("Expected automatic PBC row based on site boundary.");

    AssertEqual(report.BioAreaSquareMeters, automaticPbc.AreaSquareMeters, 0.0001);

    AreaBalanceRow existingBuilding = report.Rows.FirstOrDefault(row =>
        row.Category == PztCategories.Building &&
        row.Status.Equals("Istniejaca", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("Expected existing building row in balance.");

    AssertEqual(75, existingBuilding.AreaSquareMeters, 0.0001);
}
static void BalanceCreatesPerPlotReports()
{
    PztAreaItem[] items =
    [
        new(PztCategories.SiteBoundary, string.Empty, 1000, 0, 0, 0, "A"),
        new(PztCategories.Building, "Projektowana", 200, 0, 2, 3, "A"),
        new(PztCategories.AccessRoad, "Projektowana", 100, 0, 0, 0, "A"),
        new(PztCategories.SiteBoundary, string.Empty, 500, 0, 0, 0, "B"),
        new(PztCategories.Building, "Istniejaca", 50, 0, 1, 3, "B"),
        new(PztCategories.Parking, "Projektowana", 25, 0, 0, 0, "B")
    ];

    UrbanReport report = PztBalanceService.BuildUrbanReport(items, new MpzpRequirements(0, 0, 0, 0, 0), ParkingSettings.Default);

    AssertEqual(1500, report.SiteAreaSquareMeters, 0.0001);
    AssertEqual(250, report.BuildingFootprintSquareMeters, 0.0001);
    AssertEqual(2, report.PlotReportsSafe.Count, 0.0001);

    UrbanReport plotA = report.PlotReportsSafe.First(plot => plot.PlotId == "A").Report;
    UrbanReport plotB = report.PlotReportsSafe.First(plot => plot.PlotId == "B").Report;

    AssertEqual(1000, plotA.SiteAreaSquareMeters, 0.0001);
    AssertEqual(200, plotA.BuildingFootprintSquareMeters, 0.0001);
    AssertEqual(700, plotA.BioAreaSquareMeters, 0.0001);
    AssertEqual(500, plotB.SiteAreaSquareMeters, 0.0001);
    AssertEqual(50, plotB.BuildingFootprintSquareMeters, 0.0001);
    AssertEqual(425, plotB.BioAreaSquareMeters, 0.0001);
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

static void DocxExportCreatesStyledPabTables()
{
    UrbanReport report = BuildSampleReport();
    string filePath = Path.Combine(Path.GetTempPath(), $"pzt-docx-test-{Guid.NewGuid():N}.docx");

    try
    {
        PztDocxExporter.ExportBalance(report, "test-build", filePath);

        using ZipArchive archive = ZipFile.OpenRead(filePath);
        ZipArchiveEntry documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidOperationException("DOCX does not contain word/document.xml.");

        using Stream stream = documentEntry.Open();
        XDocument document = XDocument.Load(stream);
        string xml = document.ToString(SaveOptions.DisableFormatting);

        AssertTextContains(xml, "Bilans terenu PZT");
        AssertTextContains(xml, "Bilans powierzchni terenu");
        AssertTextContains(xml, "Sprawdzenie wymagan MPZP");
        AssertTextContains(xml, "D9E8E7");
        AssertTextContains(xml, "DCEEDC");
        AssertTextContains(xml, "Elementy projektowane");
        AssertTextContains(xml, "Elementy istniejace");
        AssertTextContains(xml, "Suma - elementy projektowane");
        AssertTextContains(xml, "Suma - elementy istniejace");
        AssertTextContains(xml, "Razem projektowane i istniejace");
    }
    finally
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
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

static void AssertTextContains(string value, string expected)
{
    if (!value.Contains(expected, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Expected generated DOCX XML to contain `{expected}`.");
    }
}
