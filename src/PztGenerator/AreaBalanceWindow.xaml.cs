using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace PztGenerator;

public partial class AreaBalanceWindow : Window
{
    private readonly AreaBalanceViewModel viewModel;
    private readonly UIApplication application;
    private readonly Document document;

    public AreaBalanceWindow(UrbanReport report, UIApplication application, Document document)
    {
        InitializeComponent();
        this.application = application;
        this.document = document;
        viewModel = new AreaBalanceViewModel(report);
        DataContext = viewModel;

        WindowInteropHelperOwner.TrySetOwner(this, application);
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Eksport bilansu PZT",
            FileName = $"Bilans_PZT_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv",
            Filter = "CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
            DefaultExt = ".csv",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, viewModel.ToCsv(), Encoding.UTF8);
        MessageBox.Show(this, "Wyeksportowano bilans CSV.", "PZT", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveMpzpButton_Click(object sender, RoutedEventArgs e)
    {
        if (!viewModel.MpzpSettings.TryBuildRequirements(out MpzpRequirements requirements, out string error))
        {
            MessageBox.Show(this, error, "PZT - MPZP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using Transaction transaction = new(document, "Ustaw wymagania MPZP");
        transaction.Start();

        Element projectInformation = document.ProjectInformation;
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinBioPercent, requirements.MinBioPercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinBuildingCoveragePercent, requirements.MinBuildingCoveragePercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMaxBuildingCoveragePercent, requirements.MaxBuildingCoveragePercent);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMinIntensity, requirements.MinIntensity);
        PztParameterValue.WriteDouble(projectInformation, PztParameterNames.MpzpMaxIntensity, requirements.MaxIntensity);

        transaction.Commit();

        MessageBox.Show(this, "Zapisano wymagania MPZP. Uruchom ponownie bilans, aby przeliczyc walidacje dla nowych wartosci.", "PZT - MPZP", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyGraphicsButton_Click(object sender, RoutedEventArgs e)
    {
        using Transaction transaction = new(document, "Odswiez style PZT");
        transaction.Start();

        PztGraphicsApplyResult result = PztGraphicsStyler.ApplyProjectDefaults(document);

        transaction.Commit();

        MessageBox.Show(this, $"Zastosowano style graficzne PZT: {result.StyleCount}. Zaktualizowano obwiednie regionow: {result.RegionCount}.", "PZT - grafika", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplySelectedTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (TypeRowsGrid.SelectedItem is not PztTypeSettingsViewModel selectedType)
        {
            MessageBox.Show(this, "Wybierz typ PZT z tabeli.", "PZT - typy", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selectedType.TryBuildPreset(out PztPreset preset, out string error))
        {
            MessageBox.Show(this, error, "PZT - typy", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        List<Element> selectedElements = application.ActiveUIDocument.Selection.GetElementIds()
            .Select(document.GetElement)
            .Where(IsSupportedPztElement)
            .ToList();

        if (selectedElements.Count == 0)
        {
            MessageBox.Show(this, "Zaznacz w Revit jeden lub kilka obszarow PZT albo regionow wypelnienia.", "PZT - typy", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using Transaction transaction = new(document, "Zastosuj typ PZT");
        transaction.Start();

        foreach (Element element in selectedElements)
        {
            PztParameterValue.WriteString(element, PztParameterNames.Category, preset.Category);
            PztParameterValue.WriteString(element, PztParameterNames.Status, preset.Status);
            PztParameterValue.WriteDouble(element, PztParameterNames.BioFactor, preset.BioFactor);
            PztParameterValue.WriteDouble(element, PztParameterNames.Floors, preset.Floors);
            PztParameterValue.WriteDouble(element, PztParameterNames.StoreyHeight, preset.StoreyHeight);
            PztParameterValue.WriteString(element, PztParameterNames.Notes, selectedType.Notes.Trim());
            PztElementDataStorage.Write(element, preset);
            PztGraphicsStyler.Apply(element, preset);
        }

        transaction.Commit();

        MessageBox.Show(this, $"Zastosowano typ `{preset.Name}` do elementow PZT: {selectedElements.Count}. Uruchom ponownie bilans, aby odswiezyc raport.", "PZT - typy", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static bool IsSupportedPztElement(Element? element)
    {
        return element is Area area && area.Area > 0
            || element is FilledRegion;
    }
}

public sealed class AreaBalanceViewModel
{
    private readonly IReadOnlyCollection<AreaBalanceRow> sourceRows;
    private readonly UrbanReport report;

    public AreaBalanceViewModel(UrbanReport report)
    {
        this.report = report;
        sourceRows = report.Rows;
        Rows = new ObservableCollection<AreaBalanceRowViewModel>(
            report.Rows.Select(row => new AreaBalanceRowViewModel(row, report)));

        SummaryText = $"{report.Rows.Sum(row => row.AreaCount)} obszarow, powierzchnia dzialki: {FormatSquareMeters(report.SiteAreaSquareMeters)}";
        TotalAreaText = FormatSquareMeters(report.TotalAreaSquareMeters);
        TotalBioAreaText = FormatSquareMeters(report.BioAreaSquareMeters);
        BuildingFootprintText = FormatSquareMeters(report.BuildingFootprintSquareMeters);
        BuildingCoverageText = $"{report.BuildingCoveragePercent:N2}% pow. dzialki";
        HardenedAreaText = FormatSquareMeters(report.HardenedAreaSquareMeters);
        BioAreaText = FormatSquareMeters(report.BioAreaSquareMeters);
        BioPercentText = $"{report.BioPercent:N2}% pow. dzialki";
        IntensityText = report.Intensity.ToString("N2", CultureInfo.CurrentCulture);
        GrossFloorAreaText = $"{FormatSquareMeters(report.GrossFloorAreaSquareMeters)} pow. calk.";
        ParkingSpacesText = $"Razem: {report.ParkingSpaceCount:N0}";
        ParkingAreaText = $"Zwykle: {report.RegularParkingSpaceCount:N0}, N: {report.AccessibleParkingSpaceCount:N0}";
        ParkingSettingsText = $"Zwykle miejsce: {report.ParkingSettings.RegularSpaceWidthMeters:N2} x {report.ParkingSettings.RegularSpaceLengthMeters:N2} m = {report.ParkingSettings.RegularSpaceAreaSquareMeters:N2} m2. Miejsce N: {report.ParkingSettings.AccessibleSpaceWidthMeters:N2} x {report.ParkingSettings.AccessibleSpaceLengthMeters:N2} m = {report.ParkingSettings.AccessibleSpaceAreaSquareMeters:N2} m2.";
        MpzpSettings = new MpzpSettingsViewModel(report.Requirements, report.SiteAreaSquareMeters);
        BuildText = $"Build: {GetBuildText()}";
        PrototypeNoticeText = "MVP / prototyp testowy - wyniki sluza do sprawdzenia workflow i logiki bilansu, nie do finalnej dokumentacji.";
        ValidationMessages = new ObservableCollection<ValidationMessageViewModel>(
            report.ValidationMessages.Select(message => new ValidationMessageViewModel(message)));
        GraphicRows = new ObservableCollection<PztGraphicStyleViewModel>(
            PztGraphicsStyler.DefaultStyles.Select(style => new PztGraphicStyleViewModel(style)));
        TypeRows = new ObservableCollection<PztTypeSettingsViewModel>(
            PztPreset.All.Select(preset => new PztTypeSettingsViewModel(preset)));
    }

    public ObservableCollection<AreaBalanceRowViewModel> Rows { get; }

    public string SummaryText { get; }

    public string TotalAreaText { get; }

    public string TotalBioAreaText { get; }

    public string BuildingFootprintText { get; }

    public string BuildingCoverageText { get; }

    public string HardenedAreaText { get; }

    public string BioAreaText { get; }

    public string BioPercentText { get; }

    public string IntensityText { get; }

    public string GrossFloorAreaText { get; }

    public string ParkingSpacesText { get; }

    public string ParkingAreaText { get; }

    public string ParkingSettingsText { get; }

    public MpzpSettingsViewModel MpzpSettings { get; }

    public string BuildText { get; }

    public string PrototypeNoticeText { get; }

    public ObservableCollection<ValidationMessageViewModel> ValidationMessages { get; }

    public ObservableCollection<PztGraphicStyleViewModel> GraphicRows { get; }

    public ObservableCollection<PztTypeSettingsViewModel> TypeRows { get; }

    internal static string FormatSquareMeters(double value)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0:N2} m2", value);
    }

    public string ToCsv()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Kategoria;Status;Szt.;Powierzchnia [m2];Udzial dzialki [%];Informacje");

        foreach (AreaBalanceRow row in sourceRows)
        {
            var rowViewModel = new AreaBalanceRowViewModel(row, report);
            builder.AppendLine(string.Join(
                ";",
                EscapeCsv(row.Category),
                EscapeCsv(row.Status),
                row.AreaCount.ToString(CultureInfo.CurrentCulture),
                FormatNumber(row.AreaSquareMeters),
                EscapeCsv(rowViewModel.SiteShareText),
                EscapeCsv(rowViewModel.DetailsText)));
        }

        builder.AppendLine(string.Join(
            ";",
            "Razem",
            string.Empty,
            sourceRows.Sum(row => row.AreaCount).ToString(CultureInfo.CurrentCulture),
            FormatNumber(report.TotalAreaSquareMeters),
            string.Empty,
            string.Empty));

        builder.AppendLine();
        builder.AppendLine("Wskaznik;Wartosc");
        builder.AppendLine($"Powierzchnia dzialki [m2];{FormatNumber(report.SiteAreaSquareMeters)}");
        builder.AppendLine($"Powierzchnia zabudowy [m2];{FormatNumber(report.BuildingFootprintSquareMeters)}");
        builder.AppendLine($"Powierzchnia utwardzona [m2];{FormatNumber(report.HardenedAreaSquareMeters)}");
        builder.AppendLine($"Wskaznik pow. zabudowy [%];{FormatNumber(report.BuildingCoveragePercent)}");
        builder.AppendLine($"Powierzchnia biologicznie czynna [m2];{FormatNumber(report.BioAreaSquareMeters)}");
        builder.AppendLine($"Wskaznik PBC [%];{FormatNumber(report.BioPercent)}");
        builder.AppendLine($"Powierzchnia calkowita [m2];{FormatNumber(report.GrossFloorAreaSquareMeters)}");
        builder.AppendLine($"Intensywnosc zabudowy;{FormatNumber(report.Intensity)}");
        builder.AppendLine($"Powierzchnia parkingow [m2];{FormatNumber(report.ParkingAreaSquareMeters)}");
        builder.AppendLine($"Liczba miejsc parkingowych [szt.];{report.ParkingSpaceCount.ToString(CultureInfo.CurrentCulture)}");
        builder.AppendLine($"Liczba miejsc dla niepelnosprawnych [szt.];{report.AccessibleParkingSpaceCount.ToString(CultureInfo.CurrentCulture)}");
        builder.AppendLine($"Pow. zwyklego miejsca [m2];{FormatNumber(report.ParkingSettings.RegularSpaceAreaSquareMeters)}");
        builder.AppendLine($"Pow. miejsca N [m2];{FormatNumber(report.ParkingSettings.AccessibleSpaceAreaSquareMeters)}");

        builder.AppendLine();
        builder.AppendLine("Walidacja MPZP");

        foreach (ValidationMessage message in report.ValidationMessages)
        {
            builder.AppendLine(EscapeCsv(message.Text));
        }

        return builder.ToString();
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string GetBuildText()
    {
        var attribute = typeof(App).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();

        return attribute?.InformationalVersion
            ?? typeof(App).Assembly.GetName().Version?.ToString()
            ?? "dev";
    }
}

public sealed class PztTypeSettingsViewModel
{
    public PztTypeSettingsViewModel(PztPreset preset)
    {
        Name = preset.Name;
        Category = preset.Category;
        StatusRaw = preset.Status;
        Status = string.IsNullOrWhiteSpace(preset.Status) ? "-" : preset.Status;
        Floors = Format(preset.Floors);
        StoreyHeight = Format(preset.StoreyHeight);
        BioFactor = Format(preset.BioFactor);
        Notes = string.Empty;
    }

    public string Name { get; }

    public string Category { get; }

    public string Status { get; }

    public string StatusRaw { get; }

    public string Floors { get; set; }

    public string StoreyHeight { get; set; }

    public string BioFactor { get; set; }

    public string Notes { get; set; }

    public bool TryBuildPreset(out PztPreset preset, out string error)
    {
        preset = new PztPreset(Name, Category, StatusRaw, 0, 0, 0);

        if (!PztParameterValue.TryParseDouble(Floors, out double floors) ||
            !PztParameterValue.TryParseDouble(StoreyHeight, out double storeyHeight) ||
            !PztParameterValue.TryParseDouble(BioFactor, out double bioFactor))
        {
            error = "Wpisz liczby w polach: kondygnacje, wysokosc kondygnacji i wspolczynnik PBC.";
            return false;
        }

        if (floors < 0 || storeyHeight < 0 || bioFactor < 0 || bioFactor > 1)
        {
            error = "Kondygnacje i wysokosc nie moga byc ujemne, a wspolczynnik PBC musi byc od 0 do 1.";
            return false;
        }

        preset = new PztPreset(Name, Category, StatusRaw, bioFactor, floors, storeyHeight);
        error = string.Empty;
        return true;
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }
}

public sealed class PztGraphicStyleViewModel
{
    public PztGraphicStyleViewModel(PztGraphicStyle style)
    {
        Name = style.Name;
        Category = style.Category;
        Status = string.IsNullOrWhiteSpace(style.Status) ? "-" : style.Status;
        LineStyleName = style.LineStyleName;
        BoundaryLineWeight = style.LineWeight;
        ColorText = $"{style.FillColor.Red}, {style.FillColor.Green}, {style.FillColor.Blue}";
        LineColorText = $"{style.LineColor.Red}, {style.LineColor.Green}, {style.LineColor.Blue}";
        LinePatternText = style.PatternKind == PztLinePatternKind.Dashed ? "Przerywana" : "Ciagla";
        FillPattern = "Pelne wypelnienie";
        Description = style.Description;
        Swatch = new SolidColorBrush(System.Windows.Media.Color.FromRgb(style.FillColor.Red, style.FillColor.Green, style.FillColor.Blue));
        LineSwatch = new SolidColorBrush(System.Windows.Media.Color.FromRgb(style.LineColor.Red, style.LineColor.Green, style.LineColor.Blue));
    }

    public string Name { get; }

    public string Category { get; }

    public string Status { get; }

    public string LineStyleName { get; }

    public int BoundaryLineWeight { get; }

    public string ColorText { get; }

    public string LineColorText { get; }

    public string LinePatternText { get; }

    public string FillPattern { get; }

    public string Description { get; }

    public Brush Swatch { get; }

    public Brush LineSwatch { get; }
}

public sealed class ValidationMessageViewModel
{
    public ValidationMessageViewModel(ValidationMessage message)
    {
        Text = message.Text;
        Background = message.Severity switch
        {
            ValidationSeverity.Success => new SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 248, 239)),
            ValidationSeverity.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 235)),
            ValidationSeverity.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 248, 224)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 246, 252))
        };
        Foreground = message.Severity switch
        {
            ValidationSeverity.Success => new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 110, 55)),
            ValidationSeverity.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 35, 35)),
            ValidationSeverity.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(115, 82, 0)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 65, 85))
        };
    }

    public string Text { get; }

    public Brush Background { get; }

    public Brush Foreground { get; }
}

public sealed class AreaBalanceRowViewModel
{
    public AreaBalanceRowViewModel(AreaBalanceRow row, UrbanReport report)
    {
        Category = row.Category;
        Status = row.Status;
        AreaCount = row.AreaCount;
        AreaSquareMetersText = AreaBalanceViewModel.FormatSquareMeters(row.AreaSquareMeters);
        SiteShareText = report.SiteAreaSquareMeters > 0
            ? $"{row.AreaSquareMeters / report.SiteAreaSquareMeters * 100:N2}%"
            : "-";
        DetailsText = BuildDetailsText(row, report);
    }

    public string Category { get; }

    public string Status { get; }

    public int AreaCount { get; }

    public string AreaSquareMetersText { get; }

    public string SiteShareText { get; }

    public string DetailsText { get; }

    private static string BuildDetailsText(AreaBalanceRow row, UrbanReport report)
    {
        if (string.Equals(row.Category, PztCategories.Building, StringComparison.OrdinalIgnoreCase))
        {
            return $"Pow. calk.: {AreaBalanceViewModel.FormatSquareMeters(row.GrossFloorAreaSquareMeters)}";
        }

        if (string.Equals(row.Category, PztCategories.Parking, StringComparison.OrdinalIgnoreCase))
        {
            return $"Miejsca: {report.ParkingSpaceCount:N0}, zwykle: {report.RegularParkingSpaceCount:N0}, N: {report.AccessibleParkingSpaceCount:N0}";
        }

        if (string.Equals(row.Category, PztCategories.SiteBoundary, StringComparison.OrdinalIgnoreCase))
        {
            return "Baza do PBC";
        }

        if (string.Equals(row.Category, PztCategories.SemiPermeable, StringComparison.OrdinalIgnoreCase))
        {
            return $"Wsp. PBC: {row.BioFactorLabel}";
        }

        return string.Empty;
    }
}
