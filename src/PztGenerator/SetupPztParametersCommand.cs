using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PztGenerator;

[Transaction(TransactionMode.Manual)]
public sealed class SetupPztParametersCommand : IExternalCommand
{
    private const string SharedParameterGroupName = "PZT Generator";

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication application = commandData.Application;
        Document document = application.ActiveUIDocument.Document;

        using Transaction transaction = new(document, "Przygotuj parametry PZT");
        transaction.Start();

        int addedCount = 0;
        BuiltInCategory[] pztElementCategories = [BuiltInCategory.OST_Areas, BuiltInCategory.OST_DetailComponents];

        addedCount += EnsureParameter(application.Application, document, PztParameterNames.Category, SpecTypeId.String.Text, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.BioFactor, SpecTypeId.Number, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.Status, SpecTypeId.String.Text, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.Floors, SpecTypeId.Number, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.StoreyHeight, SpecTypeId.Number, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.Notes, SpecTypeId.String.Text, pztElementCategories) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.MpzpMinBioPercent, SpecTypeId.Number, BuiltInCategory.OST_ProjectInformation) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.MpzpMinBuildingCoveragePercent, SpecTypeId.Number, BuiltInCategory.OST_ProjectInformation) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.MpzpMaxBuildingCoveragePercent, SpecTypeId.Number, BuiltInCategory.OST_ProjectInformation) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.MpzpMinIntensity, SpecTypeId.Number, BuiltInCategory.OST_ProjectInformation) ? 1 : 0;
        addedCount += EnsureParameter(application.Application, document, PztParameterNames.MpzpMaxIntensity, SpecTypeId.Number, BuiltInCategory.OST_ProjectInformation) ? 1 : 0;

        transaction.Commit();

        string summary = addedCount == 0
            ? "Parametry PZT byly juz dodane do kategorii obszarow i regionow wypelnienia."
            : $"Dodano albo rozszerzono parametry PZT dla obszarow i regionow wypelnienia: {addedCount}.";

        TaskDialog.Show("PZT - przygotowanie", summary);
        return Result.Succeeded;
    }

    private static bool EnsureParameter(Autodesk.Revit.ApplicationServices.Application application, Document document, string name, ForgeTypeId specTypeId, BuiltInCategory builtInCategory)
    {
        return EnsureParameter(application, document, name, specTypeId, [builtInCategory]);
    }

    private static bool EnsureParameter(Autodesk.Revit.ApplicationServices.Application application, Document document, string name, ForgeTypeId specTypeId, IReadOnlyCollection<BuiltInCategory> builtInCategories)
    {
        Definition? existingDefinition = FindProjectParameterDefinition(document, name);

        if (existingDefinition is not null)
        {
            return EnsureExistingParameterCategories(application, document, existingDefinition, builtInCategories);
        }

        Definition definition = GetOrCreateDefinition(application, name, specTypeId);
        CategorySet categories = application.Create.NewCategorySet();

        foreach (BuiltInCategory builtInCategory in builtInCategories)
        {
            Category? category = Category.GetCategory(document, builtInCategory);

            if (category is not null)
            {
                categories.Insert(category);
            }
        }

        InstanceBinding binding = application.Create.NewInstanceBinding(categories);
        return document.ParameterBindings.Insert(definition, binding, GroupTypeId.IdentityData);
    }

    private static bool EnsureExistingParameterCategories(
        Autodesk.Revit.ApplicationServices.Application application,
        Document document,
        Definition definition,
        IReadOnlyCollection<BuiltInCategory> builtInCategories)
    {
        Binding? existingBinding = document.ParameterBindings.get_Item(definition);

        if (existingBinding is not ElementBinding elementBinding)
        {
            return false;
        }

        CategorySet categories = application.Create.NewCategorySet();

        foreach (Category existingCategory in elementBinding.Categories)
        {
            categories.Insert(existingCategory);
        }

        bool changed = false;

        foreach (BuiltInCategory builtInCategory in builtInCategories)
        {
            Category? category = Category.GetCategory(document, builtInCategory);

            if (category is not null && !ContainsCategory(categories, category))
            {
                categories.Insert(category);
                changed = true;
            }
        }

        if (!changed)
        {
            return false;
        }

        InstanceBinding binding = application.Create.NewInstanceBinding(categories);
        return document.ParameterBindings.ReInsert(definition, binding, GroupTypeId.IdentityData);
    }

    private static bool ContainsCategory(CategorySet categories, Category category)
    {
        return categories
            .Cast<Category>()
            .Any(existingCategory => existingCategory.Id == category.Id);
    }

    private static Definition? FindProjectParameterDefinition(Document document, string name)
    {
        DefinitionBindingMapIterator iterator = document.ParameterBindings.ForwardIterator();

        while (iterator.MoveNext())
        {
            if (string.Equals(iterator.Key.Name, name, StringComparison.Ordinal))
            {
                return iterator.Key;
            }
        }

        return null;
    }

    private static Definition GetOrCreateDefinition(Autodesk.Revit.ApplicationServices.Application application, string name, ForgeTypeId specTypeId)
    {
        string? originalSharedParameterFile = application.SharedParametersFilename;
        string sharedParameterFile = EnsureSharedParameterFile();

        try
        {
            application.SharedParametersFilename = sharedParameterFile;
            DefinitionFile definitionFile = application.OpenSharedParameterFile()
                ?? throw new InvalidOperationException("Nie udalo sie otworzyc pliku parametrow wspoldzielonych PZT.");

            DefinitionGroup group = definitionFile.Groups.get_Item(SharedParameterGroupName)
                ?? definitionFile.Groups.Create(SharedParameterGroupName);

            Definition? existingDefinition = group.Definitions
                .Cast<Definition>()
                .FirstOrDefault(definition => string.Equals(definition.Name, name, StringComparison.Ordinal));

            if (existingDefinition is not null)
            {
                return existingDefinition;
            }

            var options = new ExternalDefinitionCreationOptions(name, specTypeId)
            {
                Visible = true
            };

            return group.Definitions.Create(options);
        }
        finally
        {
            application.SharedParametersFilename = originalSharedParameterFile;
        }
    }

    private static string EnsureSharedParameterFile()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PztGenerator");

        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "PztGeneratorSharedParameters.txt");

        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty);
        }

        return path;
    }
}
