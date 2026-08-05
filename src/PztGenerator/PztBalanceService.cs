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
        IReadOnlyCollection<PlotBalanceReport> plotReports = BuildPlotReports(areaItems, requirements, parkingSettings);
        UrbanReport report = BuildReport(areaItems, requirements, parkingSettings, plotReports);
        List<ValidationMessage> messages = report.ValidationMessages.ToList();

        if (plotReports.Count > 0)
        {
            int plotlessCount = areaItems.Count(item => !item.HasPlotId);

            if (plotlessCount > 0)
            {
                messages.Insert(0, new ValidationMessage($"Elementy bez indeksu dzialki `PZT_Dzialka`: {plotlessCount}. Wejda do bilansu calosciowego, ale nie do bilansu konkretnej dzialki.", ValidationSeverity.Warning));
            }
        }

        if (unassignedCount > 0)
        {
            messages.Insert(0, new ValidationMessage($"Pominieto elementy bez typu PZT: {unassignedCount}. Zaznacz je i uzyj `Przypisz typ`, jesli maja wejsc do bilansu.", ValidationSeverity.Warning));
        }

        if (invalidCategoryCount > 0)
        {
            messages.Insert(0, new ValidationMessage($"Pominieto elementy z nieprawidlowa kategoria: {invalidCategoryCount}. Uzyj `Przypisz typ`, zamiast wpisywac wartosc recznie.", ValidationSeverity.Warning));
        }

        return report with { ValidationMessages = messages };
    }

    private static IReadOnlyCollection<PlotBalanceReport> BuildPlotReports(
        IReadOnlyCollection<PztAreaItem> areaItems,
        MpzpRequirements requirements,
        ParkingSettings parkingSettings)
    {
        return areaItems
            .Where(item => item.HasPlotId)
            .GroupBy(item => item.NormalizedPlotId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new PlotBalanceReport(
                group.Key,
                BuildReport(group.ToList(), requirements, parkingSettings, Array.Empty<PlotBalanceReport>())))
            .ToList();
    }

    private static UrbanReport BuildReport(
        IReadOnlyCollection<PztAreaItem> areaItems,
        MpzpRequirements requirements,
        ParkingSettings parkingSettings,
        IReadOnlyCollection<PlotBalanceReport> plotReports)
    {
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
        List<AreaBalanceRow> rows = BuildRows(areaItems, siteArea, bioArea);
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
            messages,
            plotReports);
    }

    private static List<AreaBalanceRow> BuildRows(IReadOnlyCollection<PztAreaItem> areaItems, double siteArea, double bioArea)
    {
        List<AreaBalanceRow> rows = areaItems
            .Where(item => siteArea <= 0 || !string.Equals(item.Category, PztCategories.BioActive, StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => new { item.Category, item.Status })
            .Select(group => new AreaBalanceRow(
                group.Key.Category,
                group.Key.Status,
                group.Count(),
                group.Sum(item => item.AreaSquareMeters),
                group.Sum(item => item.GrossFloorAreaSquareMeters),
                group.Sum(item => item.BioAreaSquareMeters),
                GetFactorLabel(group)))
            .ToList();

        if (siteArea > 0)
        {
            rows.Add(new AreaBalanceRow(
                PztCategories.BioActive,
                "Automatyczna z granicy dzialki",
                1,
                bioArea,
                0,
                bioArea,
                "1,00"));
        }

        return rows
            .OrderBy(row => GetRowSortKey(row.Category))
            .ThenBy(row => row.Status)
            .ToList();
    }

    private static int GetRowSortKey(string category)
    {
        return category switch
        {
            PztCategories.SiteBoundary => 0,
            PztCategories.Building => 1,
            PztCategories.AccessRoad => 2,
            PztCategories.Walkway => 3,
            PztCategories.Parking => 4,
            PztCategories.SemiPermeable => 5,
            PztCategories.BioActive => 6,
            _ => 99
        };
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
