namespace PztGenerator;

public static class PztBalanceService
{
    public static UrbanReport BuildUrbanReport(
        IReadOnlyCollection<PztAreaItem> allAreaItems,
        MpzpRequirements requirements,
        ParkingSettings parkingSettings)
    {
        int unassignedCount = allAreaItems.Count(item => item.IsUnassigned);
        int invalidCategoryCount = allAreaItems.Count(item => item.HasInvalidCategory);
        List<PztAreaItem> areaItems = allAreaItems
            .Where(item => !item.IsUnassigned && !item.HasInvalidCategory)
            .ToList();

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

        double siteArea = Math.Max(0, areaItems.Where(item => item.IsSiteBoundary).Sum(item => item.AreaSquareMeters));
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
        List<ValidationMessage> messages = MpzpValidationService.Validate(
            requirements,
            siteArea,
            buildingFootprint,
            bioArea,
            grossFloorArea,
            buildingCoveragePercent,
            bioPercent,
            intensity);

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

    private static string GetFactorLabel(IEnumerable<PztAreaItem> areas)
    {
        List<double> factors = areas
            .Select(area => area.BioFactor)
            .DistinctBy(factor => Math.Round(factor, 4))
            .ToList();

        return factors.Count == 1 ? factors[0].ToString("N2") : "rozne";
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
