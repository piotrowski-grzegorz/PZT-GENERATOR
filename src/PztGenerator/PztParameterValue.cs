using System.Globalization;
using Autodesk.Revit.DB;

namespace PztGenerator;

internal static class PztParameterValue
{
    public static string ReadString(Element element, string parameterName)
    {
        return element.LookupParameter(parameterName)?.AsString()?.Trim() ?? string.Empty;
    }

    public static double ReadDouble(Element element, string parameterName)
    {
        Parameter? parameter = element.LookupParameter(parameterName);

        if (parameter is null || !parameter.HasValue)
        {
            return 0;
        }

        if (parameter.StorageType == StorageType.Double)
        {
            return parameter.AsDouble();
        }

        string? value = parameter.AsString();
        return TryParseDouble(value, out double parsedValue) ? parsedValue : 0;
    }

    public static void WriteString(Element element, string parameterName, string value)
    {
        Parameter? parameter = element.LookupParameter(parameterName);

        if (parameter is not null && !parameter.IsReadOnly)
        {
            parameter.Set(value);
        }
    }

    public static void WriteDouble(Element element, string parameterName, double value)
    {
        Parameter? parameter = element.LookupParameter(parameterName);

        if (parameter is null || parameter.IsReadOnly)
        {
            return;
        }

        if (parameter.StorageType == StorageType.Double)
        {
            parameter.Set(value);
        }
        else if (parameter.StorageType == StorageType.String)
        {
            parameter.Set(value.ToString("0.##", CultureInfo.CurrentCulture));
        }
    }

    public static bool TryParseDouble(string? value, out double result)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
        {
            return true;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
