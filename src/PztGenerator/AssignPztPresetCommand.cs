using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PztGenerator;

[Transaction(TransactionMode.Manual)]
public sealed class AssignPztPresetCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        List<Element> selectedElements = uiDocument.Selection.GetElementIds()
            .Select(document.GetElement)
            .Where(IsSupportedPztElement)
            .ToList();

        if (selectedElements.Count == 0)
        {
            TaskDialog.Show("PZT - przypisz typ", "Zaznacz jeden lub kilka obszarow PZT albo regionow wypelnienia, a potem uruchom polecenie ponownie.");
            return Result.Succeeded;
        }

        var window = new AssignPztPresetWindow(selectedElements.Count, commandData.Application);

        if (window.ShowDialog() != true || window.SelectedPreset is null)
        {
            return Result.Cancelled;
        }

        using Transaction transaction = new(document, "Przypisz typ PZT");
        transaction.Start();

        foreach (Element element in selectedElements)
        {
            PztParameterValue.WriteString(element, PztParameterNames.Category, window.SelectedPreset.Category);
            PztParameterValue.WriteString(element, PztParameterNames.Status, window.SelectedPreset.Status);
            PztParameterValue.WriteDouble(element, PztParameterNames.BioFactor, window.SelectedPreset.BioFactor);
            PztParameterValue.WriteDouble(element, PztParameterNames.Floors, window.SelectedPreset.Floors);
            PztParameterValue.WriteDouble(element, PztParameterNames.StoreyHeight, window.SelectedPreset.StoreyHeight);
            PztElementDataStorage.Write(element, window.SelectedPreset);
            PztGraphicsStyler.Apply(element, window.SelectedPreset);
        }

        transaction.Commit();

        TaskDialog.Show("PZT - przypisz typ", $"Przypisano typ `{window.SelectedPreset.Name}` do elementow PZT: {selectedElements.Count}.");
        return Result.Succeeded;
    }

    private static bool IsSupportedPztElement(Element? element)
    {
        return element is Area area && area.Area > 0
            || element is FilledRegion;
    }
}
