using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Globalization;

namespace PztGenerator;

internal static class PztElementDataStorage
{
    private static readonly Guid SchemaGuid = new("47F67101-63B6-4F52-8F07-8B594ACBD6B4");

    public static void Write(Element element, PztPreset preset)
    {
        Schema schema = GetOrCreateSchema();
        var entity = new Entity(schema);

        entity.Set(schema.GetField(nameof(PztParameterNames.Category)), preset.Category);
        entity.Set(schema.GetField(nameof(PztParameterNames.Status)), preset.Status);
        entity.Set(schema.GetField(nameof(PztParameterNames.BioFactor)), ToStorageText(preset.BioFactor));
        entity.Set(schema.GetField(nameof(PztParameterNames.Floors)), ToStorageText(preset.Floors));
        entity.Set(schema.GetField(nameof(PztParameterNames.StoreyHeight)), ToStorageText(preset.StoreyHeight));

        element.SetEntity(entity);
    }

    public static string ReadString(Element element, string parameterName)
    {
        Entity entity = GetEntity(element);

        if (!entity.IsValid())
        {
            return string.Empty;
        }

        Field? field = MapField(entity.Schema, parameterName);
        return field is null ? string.Empty : entity.Get<string>(field);
    }

    public static double ReadDouble(Element element, string parameterName)
    {
        Entity entity = GetEntity(element);

        if (!entity.IsValid())
        {
            return 0;
        }

        Field? field = MapField(entity.Schema, parameterName);
        return field is null ? 0 : FromStorageText(entity.Get<string>(field));
    }

    private static Entity GetEntity(Element element)
    {
        Schema? schema = Schema.Lookup(SchemaGuid);
        return schema is null ? new Entity() : element.GetEntity(schema);
    }

    private static Field? MapField(Schema schema, string parameterName)
    {
        string? fieldName = parameterName switch
        {
            PztParameterNames.Category => nameof(PztParameterNames.Category),
            PztParameterNames.Status => nameof(PztParameterNames.Status),
            PztParameterNames.BioFactor => nameof(PztParameterNames.BioFactor),
            PztParameterNames.Floors => nameof(PztParameterNames.Floors),
            PztParameterNames.StoreyHeight => nameof(PztParameterNames.StoreyHeight),
            _ => null
        };

        return fieldName is null ? null : schema.GetField(fieldName);
    }

    private static Schema GetOrCreateSchema()
    {
        Schema? existingSchema = Schema.Lookup(SchemaGuid);

        if (existingSchema is not null)
        {
            return existingSchema;
        }

        var builder = new SchemaBuilder(SchemaGuid);
        builder.SetSchemaName("PztGeneratorElementDataText");
        builder.SetReadAccessLevel(AccessLevel.Public);
        builder.SetWriteAccessLevel(AccessLevel.Public);
        builder.AddSimpleField(nameof(PztParameterNames.Category), typeof(string));
        builder.AddSimpleField(nameof(PztParameterNames.Status), typeof(string));
        builder.AddSimpleField(nameof(PztParameterNames.BioFactor), typeof(string));
        builder.AddSimpleField(nameof(PztParameterNames.Floors), typeof(string));
        builder.AddSimpleField(nameof(PztParameterNames.StoreyHeight), typeof(string));

        return builder.Finish();
    }

    private static string ToStorageText(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static double FromStorageText(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : 0;
    }
}
