using System.Windows;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;

namespace PztGenerator;

public partial class MpzpSettingsWindow : Window
{
    private readonly MpzpSettingsViewModel viewModel;

    public MpzpSettingsWindow(MpzpRequirements requirements, double siteAreaSquareMeters, UIApplication application)
    {
        InitializeComponent();
        viewModel = new MpzpSettingsViewModel(requirements, siteAreaSquareMeters);
        DataContext = viewModel;
        WindowInteropHelperOwner.TrySetOwner(this, application);
    }

    public MpzpRequirements? Requirements { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!viewModel.TryBuildRequirements(out MpzpRequirements requirements, out string error))
        {
            MessageBox.Show(this, error, "PZT - MPZP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Requirements = requirements;
        DialogResult = true;
        Close();
    }
}

public sealed class MpzpSettingsViewModel : INotifyPropertyChanged
{
    private readonly double siteAreaSquareMeters;
    private string minBioPercent = string.Empty;
    private string minBuildingCoveragePercent = string.Empty;
    private string maxBuildingCoveragePercent = string.Empty;
    private string minIntensity = string.Empty;
    private string maxIntensity = string.Empty;

    public MpzpSettingsViewModel(MpzpRequirements requirements, double siteAreaSquareMeters)
    {
        this.siteAreaSquareMeters = siteAreaSquareMeters;
        MinBioPercent = Format(requirements.MinBioPercent);
        MinBuildingCoveragePercent = Format(requirements.MinBuildingCoveragePercent);
        MaxBuildingCoveragePercent = Format(requirements.MaxBuildingCoveragePercent);
        MinIntensity = Format(requirements.MinIntensity);
        MaxIntensity = Format(requirements.MaxIntensity);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SiteAreaText => siteAreaSquareMeters > 0
        ? $"Powierzchnia dzialki z obszaru `Granica terenu / dzialki`: {siteAreaSquareMeters:N2} m2"
        : "Brak obszaru `Granica terenu / dzialki`. Wymagane m2 pojawia sie po przypisaniu granicy dzialki.";

    public string MinBioPercent
    {
        get => minBioPercent;
        set
        {
            minBioPercent = value;
            NotifyCalculatedValues();
        }
    }

    public string MinBuildingCoveragePercent
    {
        get => minBuildingCoveragePercent;
        set
        {
            minBuildingCoveragePercent = value;
            NotifyCalculatedValues();
        }
    }

    public string MaxBuildingCoveragePercent
    {
        get => maxBuildingCoveragePercent;
        set
        {
            maxBuildingCoveragePercent = value;
            NotifyCalculatedValues();
        }
    }

    public string MinIntensity
    {
        get => minIntensity;
        set
        {
            minIntensity = value;
            NotifyCalculatedValues();
        }
    }

    public string MaxIntensity
    {
        get => maxIntensity;
        set
        {
            maxIntensity = value;
            NotifyCalculatedValues();
        }
    }

    public string RequiredMinBioAreaText => FormatPercentArea(MinBioPercent, "min.");

    public string RequiredMinBuildingAreaText => FormatPercentArea(MinBuildingCoveragePercent, "min.");

    public string AllowedMaxBuildingAreaText => FormatPercentArea(MaxBuildingCoveragePercent, "max.");

    public string RequiredMinGfaText => FormatIntensityArea(MinIntensity, "min.");

    public string AllowedMaxGfaText => FormatIntensityArea(MaxIntensity, "max.");

    public bool TryBuildRequirements(out MpzpRequirements requirements, out string error)
    {
        requirements = new MpzpRequirements(0, 0, 0, 0, 0);
        error = string.Empty;

        if (!TryParse(MinBioPercent, out double minBioPercent) ||
            !TryParse(MinBuildingCoveragePercent, out double minBuildingCoveragePercent) ||
            !TryParse(MaxBuildingCoveragePercent, out double maxBuildingCoveragePercent) ||
            !TryParse(MinIntensity, out double minIntensity) ||
            !TryParse(MaxIntensity, out double maxIntensity))
        {
            error = "Wpisz wartosci liczbowe. Mozesz uzyc przecinka dziesietnego.";
            return false;
        }

        requirements = new MpzpRequirements(
            minBioPercent,
            minBuildingCoveragePercent,
            maxBuildingCoveragePercent,
            minIntensity,
            maxIntensity);

        return true;
    }

    private static bool TryParse(string value, out double result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return true;
        }

        return PztParameterValue.TryParseDouble(value, out result);
    }

    private static string Format(double value)
    {
        return value == 0 ? string.Empty : value.ToString("0.##");
    }

    private string FormatPercentArea(string percentText, string label)
    {
        if (siteAreaSquareMeters <= 0 || !TryParse(percentText, out double percent) || percent <= 0)
        {
            return string.Empty;
        }

        return $"{label} {siteAreaSquareMeters * percent / 100:N2} m2";
    }

    private string FormatIntensityArea(string intensityText, string label)
    {
        if (siteAreaSquareMeters <= 0 || !TryParse(intensityText, out double intensity) || intensity <= 0)
        {
            return string.Empty;
        }

        return $"{label} {siteAreaSquareMeters * intensity:N2} m2 pow. calk.";
    }

    private void NotifyCalculatedValues()
    {
        OnPropertyChanged();
        OnPropertyChanged(nameof(RequiredMinBioAreaText));
        OnPropertyChanged(nameof(RequiredMinBuildingAreaText));
        OnPropertyChanged(nameof(AllowedMaxBuildingAreaText));
        OnPropertyChanged(nameof(RequiredMinGfaText));
        OnPropertyChanged(nameof(AllowedMaxGfaText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
