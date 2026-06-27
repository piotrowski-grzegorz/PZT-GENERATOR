using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace PztGenerator;

internal static class PztElementReader
{
    public static List<PztAreaItem> ReadItems(Document document)
    {
        List<PztAreaItem> items = new();

        items.AddRange(new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_Areas)
            .WhereElementIsNotElementType()
            .OfType<Area>()
            .Where(area => area.Area > 0)
            .Select(CreateAreaItem));

        items.AddRange(new FilteredElementCollector(document)
            .OfClass(typeof(FilledRegion))
            .WhereElementIsNotElementType()
            .OfType<FilledRegion>()
            .Select(CreateFilledRegionItem)
            .Where(item => item.AreaSquareMeters > 0));

        return items;
    }

    public static double ReadSiteArea(Document document)
    {
        return ReadItems(document)
            .Where(item => item.IsSiteBoundary)
            .Sum(item => item.AreaSquareMeters);
    }

    private static PztAreaItem CreateAreaItem(Area area)
    {
        return CreateItem(
            area,
            UnitUtils.ConvertFromInternalUnits(area.Area, UnitTypeId.SquareMeters));
    }

    private static PztAreaItem CreateFilledRegionItem(FilledRegion region)
    {
        return CreateItem(region, GetFilledRegionAreaSquareMeters(region));
    }

    private static PztAreaItem CreateItem(Element element, double areaSquareMeters)
    {
        string category = ReadString(element, PztParameterNames.Category);
        string status = ReadString(element, PztParameterNames.Status);

        return new PztAreaItem(
            NormalizeCategory(category),
            status,
            areaSquareMeters,
            ReadDouble(element, PztParameterNames.BioFactor),
            ReadDouble(element, PztParameterNames.Floors),
            ReadDouble(element, PztParameterNames.StoreyHeight));
    }

    private static string ReadString(Element element, string parameterName)
    {
        string parameterValue = PztParameterValue.ReadString(element, parameterName);
        return string.IsNullOrWhiteSpace(parameterValue)
            ? PztElementDataStorage.ReadString(element, parameterName)
            : parameterValue;
    }

    private static double ReadDouble(Element element, string parameterName)
    {
        double parameterValue = PztParameterValue.ReadDouble(element, parameterName);
        return Math.Abs(parameterValue) < 0.000001
            ? PztElementDataStorage.ReadDouble(element, parameterName)
            : parameterValue;
    }

    private static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return PztCategories.Unassigned;
        }

        return PztCategories.IsKnown(category) ? category.Trim() : PztCategories.Invalid;
    }

    private static double GetFilledRegionAreaSquareMeters(FilledRegion region)
    {
        IList<CurveLoop> boundaries = region.GetBoundaries();

        if (boundaries.Count == 0)
        {
            return 0;
        }

        List<double> loopAreas = boundaries
            .Select(GetCurveLoopArea)
            .Where(area => area > 0)
            .OrderByDescending(area => area)
            .ToList();

        if (loopAreas.Count == 0)
        {
            return 0;
        }

        double areaInternal = Math.Max(0, loopAreas[0] - loopAreas.Skip(1).Sum());
        return UnitUtils.ConvertFromInternalUnits(areaInternal, UnitTypeId.SquareMeters);
    }

    private static double GetCurveLoopArea(CurveLoop loop)
    {
        List<XYZ> points = new();

        foreach (Curve curve in loop)
        {
            IList<XYZ> tessellatedPoints = curve.Tessellate();

            foreach (XYZ point in tessellatedPoints)
            {
                if (points.Count == 0 || !point.IsAlmostEqualTo(points[^1]))
                {
                    points.Add(point);
                }
            }
        }

        if (points.Count < 3)
        {
            return 0;
        }

        if (points[0].IsAlmostEqualTo(points[^1]))
        {
            points.RemoveAt(points.Count - 1);
        }

        double signedArea = 0;

        for (int index = 0; index < points.Count; index++)
        {
            XYZ current = points[index];
            XYZ next = points[(index + 1) % points.Count];
            signedArea += current.X * next.Y - next.X * current.Y;
        }

        return Math.Abs(signedArea) / 2;
    }
}
