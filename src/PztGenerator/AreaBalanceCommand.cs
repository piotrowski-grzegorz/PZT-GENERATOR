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
        UrbanReport report = PztBalanceService.BuildUrbanReport(
            allAreaItems,
            MpzpSettingsCommand.ReadRequirements(document),
            ParkingSettings.Default);

        if (report.Rows.Count == 0)
        {
            TaskDialog.Show("PZT - bilans obszarow", "Nie znaleziono obszarow ani regionow wypelnienia do przeliczenia.");
            return Result.Succeeded;
        }

        var window = new AreaBalanceWindow(report, commandData.Application, document);
        window.ShowDialog();

        return Result.Succeeded;
    }
}
