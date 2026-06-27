using System.Reflection;
using System.Windows.Media;
using Autodesk.Revit.UI;

namespace PztGenerator;

public sealed class App : IExternalApplication
{
    private const string TabName = "PZT";
    private const string PanelName = "Bilans";

    public Result OnStartup(UIControlledApplication application)
    {
        CreateRibbon(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private static void CreateRibbon(UIControlledApplication application)
    {
        try
        {
            application.CreateRibbonTab(TabName);
        }
        catch
        {
            // Revit throws when the tab already exists, for example after reloads.
        }

        RibbonPanel panel = application
            .GetRibbonPanels(TabName)
            .FirstOrDefault(p => p.Name == PanelName)
            ?? application.CreateRibbonPanel(TabName, PanelName);

        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        var setupButtonData = new PushButtonData(
            "PztSetup",
            "Przygotuj\nPZT",
            assemblyPath,
            typeof(SetupPztParametersCommand).FullName);

        setupButtonData.ToolTip = "Dodaje podstawowe parametry PZT do obszarow Revita.";
        setupButtonData.LargeImage = RibbonIconFactory.Create("P", Color.FromRgb(31, 111, 235));
        setupButtonData.Image = setupButtonData.LargeImage;

        AddButtonIfMissing(panel, setupButtonData);

        var assignButtonData = new PushButtonData(
            "PztAssignPreset",
            "Przypisz\ntyp",
            assemblyPath,
            typeof(AssignPztPresetCommand).FullName);

        assignButtonData.ToolTip = "Przypisuje zaznaczonym obszarom typ PZT i domyslne parametry.";
        assignButtonData.LargeImage = RibbonIconFactory.Create("T", Color.FromRgb(30, 138, 83));
        assignButtonData.Image = assignButtonData.LargeImage;

        AddButtonIfMissing(panel, assignButtonData);

        var mpzpButtonData = new PushButtonData(
            "PztMpzpSettings",
            "MPZP",
            assemblyPath,
            typeof(MpzpSettingsCommand).FullName);

        mpzpButtonData.ToolTip = "Ustawia wymagania MPZP używane do walidacji bilansu PZT.";
        mpzpButtonData.LargeImage = RibbonIconFactory.Create("M", Color.FromRgb(217, 119, 6));
        mpzpButtonData.Image = mpzpButtonData.LargeImage;

        AddButtonIfMissing(panel, mpzpButtonData);

        var balanceButtonData = new PushButtonData(
            "PztAreaBalance",
            "Bilans\nobszarow",
            assemblyPath,
            typeof(AreaBalanceCommand).FullName);

        balanceButtonData.ToolTip = "Sumuje obszary Revita wedlug parametrow PZT.";
        balanceButtonData.LargeImage = RibbonIconFactory.Create("B", Color.FromRgb(107, 70, 193));
        balanceButtonData.Image = balanceButtonData.LargeImage;

        AddButtonIfMissing(panel, balanceButtonData);
    }

    private static void AddButtonIfMissing(RibbonPanel panel, PushButtonData buttonData)
    {
        if (panel.GetItems().All(item => item.Name != buttonData.Name))
        {
            panel.AddItem(buttonData);
        }
    }
}
