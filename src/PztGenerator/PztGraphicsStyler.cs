using Autodesk.Revit.DB;

namespace PztGenerator;

internal static class PztGraphicsStyler
{
    public static IReadOnlyList<PztGraphicStyle> DefaultStyles { get; } =
    [
        new("Granica terenu / dzialki", PztCategories.SiteBoundary, string.Empty, "pztGen_granica_terenu", new Color(220, 242, 213), new Color(38, 115, 55), 2, PztLinePatternKind.Dashed, "Zielone wypelnienie, przerywana obwiednia dzialki"),
        new("Zabudowa projektowana", PztCategories.Building, "Projektowana", "pztGen_zabudowa_proj", new Color(255, 255, 255), new Color(0, 0, 0), 8, PztLinePatternKind.Solid, "Biale wypelnienie, gruba ciagla obwiednia projektowana"),
        new("Zabudowa istniejaca", PztCategories.Building, "Istniejaca", "pztGen_zabudowa_istn", new Color(255, 255, 255), new Color(0, 0, 0), 4, PztLinePatternKind.Solid, "Biale wypelnienie, ciensza ciagla obwiednia istniejaca"),
        new("Dojazdy", PztCategories.AccessRoad, string.Empty, "pztGen_dojazdy", new Color(185, 185, 185), new Color(95, 95, 95), 3, PztLinePatternKind.Solid, "Szare wypelnienie i ciagla obwiednia dojazdu"),
        new("Dojscia", PztCategories.Walkway, string.Empty, "pztGen_dojscia", new Color(205, 205, 205), new Color(115, 115, 115), 2, PztLinePatternKind.Solid, "Jasnoszare wypelnienie i ciagla obwiednia dojscia"),
        new("Parking", PztCategories.Parking, string.Empty, "pztGen_parking", new Color(196, 222, 242), new Color(70, 105, 135), 3, PztLinePatternKind.Solid, "Lekko niebieskie wypelnienie i ciagla obwiednia parkingu"),
        new("Biologicznie czynna", PztCategories.BioActive, string.Empty, "pztGen_pbc", new Color(220, 242, 213), new Color(75, 135, 75), 2, PztLinePatternKind.Dashed, "Zielone wypelnienie i przerywana obwiednia PBC"),
        new("Czesciowo biologicznie czynna 50%", PztCategories.SemiPermeable, string.Empty, "pztGen_pbc_50", new Color(206, 232, 183), new Color(75, 135, 75), 2, PztLinePatternKind.Dashed, "Zielone wypelnienie, przerywana obwiednia PBC 50%")
    ];

    public static void Apply(Element element, PztPreset preset)
    {
        if (element is not FilledRegion region)
        {
            return;
        }

        FilledRegionType type = GetOrCreateFilledRegionType(region.Document, preset);
        PztGraphicStyle graphic = GetStyle(preset);
        ElementId lineStyleId = GetOrCreateLineStyle(region.Document, graphic);

        region.ChangeTypeId(type.Id);

        if (FilledRegion.IsValidLineStyleIdForFilledRegion(region.Document, lineStyleId))
        {
            region.SetLineStyleId(lineStyleId);
        }
    }

    public static PztGraphicsApplyResult ApplyProjectDefaults(Document document)
    {
        int styleCount = 0;
        int regionCount = 0;

        foreach (PztPreset preset in PztPreset.All)
        {
            GetOrCreateFilledRegionType(document, preset);
            GetOrCreateLineStyle(document, GetStyle(preset));
            styleCount++;
        }

        IEnumerable<FilledRegion> regions = new FilteredElementCollector(document)
            .OfClass(typeof(FilledRegion))
            .WhereElementIsNotElementType()
            .OfType<FilledRegion>();

        foreach (FilledRegion region in regions)
        {
            PztPreset? preset = FindPreset(region);

            if (preset is null)
            {
                continue;
            }

            Apply(region, preset);
            regionCount++;
        }

        return new PztGraphicsApplyResult(styleCount, regionCount);
    }

    private static FilledRegionType GetOrCreateFilledRegionType(Document document, PztPreset preset)
    {
        string typeName = $"PZT - {preset.Name}";
        FilledRegionType? existingType = new FilteredElementCollector(document)
            .OfClass(typeof(FilledRegionType))
            .Cast<FilledRegionType>()
            .FirstOrDefault(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));

        if (existingType is not null)
        {
            Configure(existingType, preset);
            return existingType;
        }

        FilledRegionType sourceType = new FilteredElementCollector(document)
            .OfClass(typeof(FilledRegionType))
            .Cast<FilledRegionType>()
            .First();

        var newType = (FilledRegionType)sourceType.Duplicate(typeName);
        Configure(newType, preset);
        return newType;
    }

    private static void Configure(FilledRegionType type, PztPreset preset)
    {
        PztGraphicStyle graphic = GetStyle(preset);
        ElementId solidFillPatternId = GetSolidFillPatternId(type.Document);

        if (solidFillPatternId != ElementId.InvalidElementId)
        {
            type.ForegroundPatternId = solidFillPatternId;
        }

        type.ForegroundPatternColor = graphic.FillColor;
        type.LineWeight = graphic.LineWeight;
    }

    private static ElementId GetSolidFillPatternId(Document document)
    {
        FillPatternElement? solidPattern = new FilteredElementCollector(document)
            .OfClass(typeof(FillPatternElement))
            .Cast<FillPatternElement>()
            .FirstOrDefault(pattern => pattern.GetFillPattern().IsSolidFill);

        return solidPattern?.Id ?? ElementId.InvalidElementId;
    }

    private static PztGraphicStyle GetStyle(PztPreset preset)
    {
        return DefaultStyles.FirstOrDefault(style =>
            string.Equals(style.Category, preset.Category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(style.Status, preset.Status, StringComparison.OrdinalIgnoreCase))
            ?? new PztGraphicStyle(preset.Name, preset.Category, preset.Status, $"pztGen_{preset.Name}", new Color(225, 225, 225), new Color(90, 90, 90), 2, PztLinePatternKind.Solid, "Domyslny styl PZT");
    }

    private static PztPreset? FindPreset(FilledRegion region)
    {
        string category = ReadString(region, PztParameterNames.Category);
        string status = ReadString(region, PztParameterNames.Status);

        PztPreset? preset = PztPreset.All.FirstOrDefault(candidate =>
            string.Equals(candidate.Category, category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.Status, status, StringComparison.OrdinalIgnoreCase));

        if (preset is not null)
        {
            return preset;
        }

        string typeName = region.Document.GetElement(region.GetTypeId())?.Name ?? string.Empty;
        const string typePrefix = "PZT - ";

        if (!typeName.StartsWith(typePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        string presetName = typeName[typePrefix.Length..];
        return PztPreset.All.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, presetName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadString(Element element, string parameterName)
    {
        string parameterValue = PztParameterValue.ReadString(element, parameterName);
        return string.IsNullOrWhiteSpace(parameterValue)
            ? PztElementDataStorage.ReadString(element, parameterName)
            : parameterValue;
    }

    private static ElementId GetOrCreateLineStyle(Document document, PztGraphicStyle style)
    {
        Category linesCategory = Category.GetCategory(document, BuiltInCategory.OST_Lines);
        Category? existingCategory = linesCategory.SubCategories
            .Cast<Category>()
            .FirstOrDefault(category => string.Equals(category.Name, style.LineStyleName, StringComparison.Ordinal));

        Category lineStyleCategory = existingCategory ?? document.Settings.Categories.NewSubcategory(linesCategory, style.LineStyleName);
        lineStyleCategory.LineColor = style.LineColor;
        lineStyleCategory.SetLineWeight(style.LineWeight, GraphicsStyleType.Projection);
        lineStyleCategory.SetLinePatternId(GetLinePatternId(document, style.PatternKind), GraphicsStyleType.Projection);

        GraphicsStyle graphicsStyle = lineStyleCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
        return graphicsStyle.Id;
    }

    private static ElementId GetLinePatternId(Document document, PztLinePatternKind patternKind)
    {
        if (patternKind == PztLinePatternKind.Solid)
        {
            return LinePatternElement.GetSolidPatternId();
        }

        const string dashedPatternName = "pztGen_przerywana";
        LinePatternElement? existingPattern = LinePatternElement.GetLinePatternElementByName(document, dashedPatternName);

        if (existingPattern is not null)
        {
            return existingPattern.Id;
        }

        var pattern = new LinePattern(dashedPatternName);
        double dashLength = UnitUtils.ConvertToInternalUnits(2.5, UnitTypeId.Millimeters);
        double spaceLength = UnitUtils.ConvertToInternalUnits(1.5, UnitTypeId.Millimeters);
        pattern.SetSegments(
        [
            new LinePatternSegment(LinePatternSegmentType.Dash, dashLength),
            new LinePatternSegment(LinePatternSegmentType.Space, spaceLength)
        ]);

        return LinePatternElement.Create(document, pattern).Id;
    }
}

public sealed record PztGraphicStyle(
    string Name,
    string Category,
    string Status,
    string LineStyleName,
    Color FillColor,
    Color LineColor,
    int LineWeight,
    PztLinePatternKind PatternKind,
    string Description);

public enum PztLinePatternKind
{
    Solid,
    Dashed
}

public sealed record PztGraphicsApplyResult(int StyleCount, int RegionCount);
