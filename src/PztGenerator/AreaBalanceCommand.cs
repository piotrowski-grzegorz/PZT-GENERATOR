using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PztGenerator;

[Transaction(TransactionMode.Manual)]
public sealed class AreaBalanceCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        List<PztAreaItem> allAreaItems = PztElementReader.ReadItems(document);
        int unassignedCount = allAreaItems.Count(item => item.IsUnassigned);
        List<PztAreaItem> areaItems = allAreaItems
            .Where(item => !item.IsUnassigned && !item.HasInvalidCategory)
            .ToList();
        double parkingArea = areaItems
            .Where(item => string.Equals(item.Category, PztCategories.Parking, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.AreaSquareMeters);
        ParkingSettings parkingSettings = ParkingSettings.Default;

        List<AreaBalanceRow> rows = areaItems
            .GroupBy(item => new { item.Category, item.Status })
            .Select(group => new AreaBalanceRow(
                group.Key.Category,
                group.Key.Status,
                group.Count(),
                group.Sum(item => item.AreaSquareMeters),
                group.Sum(item => item.GrossFloorAreaSquareMeters),
                group.Sum(item => item.BioAreaSquareMeters),
                GetFactorLabel(group)))
            .OrderBy(row => row.Category)
            .ThenBy(row => row.Status)
            .ToList();

        if (rows.Count == 0)
        {
            TaskDialog.Show("PZT - bilans obszarow", "Nie znaleziono obszarow ani regionow wypelnienia do przeliczenia.");
            return Result.Succeeded;
        }

        UrbanReport report = BuildUrbanReport(rows, areaItems, MpzpSettingsCommand.ReadRequirements(document), parkingSettings, unassignedCount, allAreaItems.Count(item => item.HasInvalidCategory));
        var window = new AreaBalanceWindow(report, commandData.Application, document);
        window.ShowDialog();

        return Result.Succeeded;
    }

    private static string GetFactorLabel(IEnumerable<PztAreaItem> areas)
    {
        List<double> factors = areas
            .Select(area => area.BioFactor)
            .DistinctBy(factor => Math.Round(factor, 4))
            .ToList();

        return factors.Count == 1 ? factors[0].ToString("N2") : "rozne";
    }

    private static UrbanReport BuildUrbanReport(
        IReadOnlyCollection<AreaBalanceRow> rows,
        IReadOnlyCollection<PztAreaItem> areaItems,
        MpzpRequirements requirements,
        ParkingSettings parkingSettings,
        int unassignedCount,
        int invalidCategoryCount)
    {
        double siteArea = areaItems.Where(item => item.IsSiteBoundary).Sum(item => item.AreaSquareMeters);

        if (siteArea <= 0)
        {
            siteArea = 0;
        }

        double buildingFootprint = areaItems.Where(item => item.IsBuilding).Sum(item => item.AreaSquareMeters);
        double hardenedArea = areaItems.Where(item => item.IsHardened).Sum(item => item.AreaSquareMeters);
        double grossFloorArea = areaItems.Where(item => item.IsBuilding).Sum(item => item.GrossFloorAreaSquareMeters);
        double semiPermeableBioArea = areaItems
            .Where(item => string.Equals(item.Category, PztCategories.SemiPermeable, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.BioAreaSquareMeters);
        double explicitBioArea = areaItems
            .Where(item => !item.IsSiteBoundary)
            .Sum(item => item.BioAreaSquareMeters);
        double bioArea = siteArea > 0
            ? Math.Max(0, siteArea - buildingFootprint - hardenedArea + semiPermeableBioArea)
            : explicitBioArea;
        double buildingCoveragePercent = siteArea > 0 ? buildingFootprint / siteArea * 100 : 0;
        double bioPercent = siteArea > 0 ? bioArea / siteArea * 100 : 0;
        double intensity = siteArea > 0 ? grossFloorArea / siteArea : 0;
        double parkingArea = areaItems
            .Where(item => string.Equals(item.Category, PztCategories.Parking, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.AreaSquareMeters);
        int regularParkingSpaceCount = CalculateRegularParkingSpaces(parkingArea, parkingSettings);
        int parkingSpaceCount = regularParkingSpaceCount + parkingSettings.AccessibleSpaceCount;
        List<ValidationMessage> messages = BuildValidationMessages(requirements, siteArea, buildingFootprint, bioArea, grossFloorArea, buildingCoveragePercent, bioPercent, intensity);

        if (unassignedCount > 0)
        {
            messages.Insert(0, new ValidationMessage($"Pominieto elementy bez typu PZT: {unassignedCount}. Zaznacz je i uzyj `Przypisz typ`, jesli maja wejsc do bilansu.", ValidationSeverity.Warning));
        }

        if (invalidCategoryCount > 0)
        {
            messages.Insert(0, new ValidationMessage($"Pominieto elementy z nieprawidlowa kategoria: {invalidCategoryCount}. Uzyj `Przypisz typ`, zamiast wpisywac wartosc recznie.", ValidationSeverity.Warning));
        }

        return new UrbanReport(
            rows,
            requirements,
            siteArea,
            buildingFootprint,
            hardenedArea,
            grossFloorArea,
            bioArea,
            buildingCoveragePercent,
            bioPercent,
            intensity,
            parkingArea,
            parkingSpaceCount,
            regularParkingSpaceCount,
            parkingSettings.AccessibleSpaceCount,
            parkingSettings,
            messages);
    }

    private static List<ValidationMessage> BuildValidationMessages(
        MpzpRequirements requirements,
        double siteArea,
        double buildingFootprint,
        double bioArea,
        double grossFloorArea,
        double buildingCoveragePercent,
        double bioPercent,
        double intensity)
    {
        var messages = new List<ValidationMessage>();

        if (siteArea <= 0)
        {
            messages.Add(new ValidationMessage("Brak powierzchni dzialki. Dodaj obszar z typem `Granica terenu / dzialki`.", ValidationSeverity.Error));
            return messages;
        }

        if (!requirements.HasAnyValue)
        {
            messages.Add(new ValidationMessage("Nie wpisano wymagan MPZP. Uzyj przycisku `MPZP`.", ValidationSeverity.Info));
            return messages;
        }

        if (buildingFootprint == 0)
        {
            messages.Add(new ValidationMessage("Brak obszarow zabudowy. Uzyj `Przypisz typ` dla obrysow budynkow.", ValidationSeverity.Warning));
        }

        if (requirements.MinBioPercent > 0 && bioPercent < requirements.MinBioPercent)
        {
            double requiredBioArea = siteArea * requirements.MinBioPercent / 100;
            messages.Add(new ValidationMessage($"PBC: {bioArea:N2} m2 / {siteArea:N2} m2 = {bioPercent:N2}%, wymagane min. {requirements.MinBioPercent:N2}% ({requiredBioArea:N2} m2) -> warunek niespelniony, brakuje {requiredBioArea - bioArea:N2} m2.", ValidationSeverity.Error));
        }
        else if (requirements.MinBioPercent > 0)
        {
            double requiredBioArea = siteArea * requirements.MinBioPercent / 100;
            messages.Add(new ValidationMessage($"PBC: {bioArea:N2} m2 / {siteArea:N2} m2 = {bioPercent:N2}%, wymagane min. {requirements.MinBioPercent:N2}% ({requiredBioArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
        }

        if (requirements.MinBuildingCoveragePercent > 0 && buildingCoveragePercent < requirements.MinBuildingCoveragePercent)
        {
            double requiredBuildingArea = siteArea * requirements.MinBuildingCoveragePercent / 100;
            messages.Add(new ValidationMessage($"Pow. zabudowy min.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, wymagane min. {requirements.MinBuildingCoveragePercent:N2}% ({requiredBuildingArea:N2} m2) -> warunek niespelniony, brakuje {requiredBuildingArea - buildingFootprint:N2} m2.", ValidationSeverity.Error));
        }
        else if (requirements.MinBuildingCoveragePercent > 0)
        {
            double requiredBuildingArea = siteArea * requirements.MinBuildingCoveragePercent / 100;
            messages.Add(new ValidationMessage($"Pow. zabudowy min.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, wymagane min. {requirements.MinBuildingCoveragePercent:N2}% ({requiredBuildingArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
        }

        if (requirements.MaxBuildingCoveragePercent > 0 && buildingCoveragePercent > requirements.MaxBuildingCoveragePercent)
        {
            double allowedBuildingArea = siteArea * requirements.MaxBuildingCoveragePercent / 100;
            messages.Add(new ValidationMessage($"Pow. zabudowy max.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, dopuszczalne max. {requirements.MaxBuildingCoveragePercent:N2}% ({allowedBuildingArea:N2} m2) -> warunek niespelniony, za duzo o {buildingFootprint - allowedBuildingArea:N2} m2.", ValidationSeverity.Error));
        }
        else if (requirements.MaxBuildingCoveragePercent > 0)
        {
            double allowedBuildingArea = siteArea * requirements.MaxBuildingCoveragePercent / 100;
            messages.Add(new ValidationMessage($"Pow. zabudowy max.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, dopuszczalne max. {requirements.MaxBuildingCoveragePercent:N2}% ({allowedBuildingArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
        }

        if (requirements.MinIntensity > 0 && intensity < requirements.MinIntensity)
        {
            double requiredGrossFloorArea = siteArea * requirements.MinIntensity;
            messages.Add(new ValidationMessage($"Intensywnosc min.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, wymagane min. {requirements.MinIntensity:N2} ({requiredGrossFloorArea:N2} m2 pow. calk.) -> warunek niespelniony, brakuje {requiredGrossFloorArea - grossFloorArea:N2} m2.", ValidationSeverity.Error));
        }
        else if (requirements.MinIntensity > 0)
        {
            double requiredGrossFloorArea = siteArea * requirements.MinIntensity;
            messages.Add(new ValidationMessage($"Intensywnosc min.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, wymagane min. {requirements.MinIntensity:N2} ({requiredGrossFloorArea:N2} m2 pow. calk.) -> warunek spelniony.", ValidationSeverity.Success));
        }

        if (requirements.MaxIntensity > 0 && intensity > requirements.MaxIntensity)
        {
            double allowedGrossFloorArea = siteArea * requirements.MaxIntensity;
            messages.Add(new ValidationMessage($"Intensywnosc max.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, dopuszczalne max. {requirements.MaxIntensity:N2} ({allowedGrossFloorArea:N2} m2 pow. calk.) -> warunek niespelniony, za duzo o {grossFloorArea - allowedGrossFloorArea:N2} m2.", ValidationSeverity.Error));
        }
        else if (requirements.MaxIntensity > 0)
        {
            double allowedGrossFloorArea = siteArea * requirements.MaxIntensity;
            messages.Add(new ValidationMessage($"Intensywnosc max.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, dopuszczalne max. {requirements.MaxIntensity:N2} ({allowedGrossFloorArea:N2} m2 pow. calk.) -> warunek spelniony.", ValidationSeverity.Success));
        }

        if (messages.Count == 0)
        {
            messages.Add(new ValidationMessage("Warunki MPZP sa spelnione.", ValidationSeverity.Success));
        }

        return messages;
    }

    private static int CalculateRegularParkingSpaces(double parkingArea, ParkingSettings settings)
    {
        double accessibleArea = settings.AccessibleSpaceCount * settings.AccessibleSpaceAreaSquareMeters;
        double regularArea = Math.Max(0, parkingArea - accessibleArea);

        if (settings.RegularSpaceAreaSquareMeters <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(regularArea / settings.RegularSpaceAreaSquareMeters);
    }
}
