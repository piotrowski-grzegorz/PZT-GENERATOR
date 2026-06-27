using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PztGenerator;

[Transaction(TransactionMode.Manual)]
public sealed class MpzpSettingsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        MpzpRequirements currentRequirements = ReadRequirements(document);
        double siteArea = ReadSiteArea(document);
        var window = new MpzpSettingsWindow(currentRequirements, siteArea, commandData.Application);

        if (window.ShowDialog() != true || window.Requirements is null)
        {
            return Result.Cancelled;
        }

        using Transaction transaction = new(document, "Ustaw wymagania MPZP");
        transaction.Start();

        Element projectInformation = document.ProjectInformation;
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinBioPercent, window.Requirements.MinBioPercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinBuildingCoveragePercent, window.Requirements.MinBuildingCoveragePercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMaxBuildingCoveragePercent, window.Requirements.MaxBuildingCoveragePercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinIntensity, window.Requirements.MinIntensity);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMaxIntensity, window.Requirements.MaxIntensity);

        transaction.Commit();

        TaskDialog.Show("PZT - MPZP", "Zapisano wymagania MPZP w informacjach o projekcie.");
        return Result.Succeeded;
    }

    public static MpzpRequirements ReadRequirements(Document document)
    {
        Element projectInformation = document.ProjectInformation;

        return new MpzpRequirements(
            PztParameterValue.ReadDouble(projectInformation, PztParameterNames.MpzpMinBioPercent),
            PztParameterValue.ReadDouble(projectInformation, PztParameterNames.MpzpMinBuildingCoveragePercent),
            PztParameterValue.ReadDouble(projectInformation, PztParameterNames.MpzpMaxBuildingCoveragePercent),
            PztParameterValue.ReadDouble(projectInformation, PztParameterNames.MpzpMinIntensity),
            PztParameterValue.ReadDouble(projectInformation, PztParameterNames.MpzpMaxIntensity));
    }

    private static double ReadSiteArea(Document document)
    {
        return PztElementReader.ReadSiteArea(document);
    }
}
