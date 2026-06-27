using System.Globalization;
using System.Windows;
using Autodesk.Revit.UI;

namespace PztGenerator;

public partial class ParkingSettingsWindow : Window
{
    private readonly ParkingSettingsViewModel viewModel;

    public ParkingSettingsWindow(double parkingAreaSquareMeters, UIApplication application)
    {
        InitializeComponent();
        viewModel = new ParkingSettingsViewModel(parkingAreaSquareMeters);
        DataContext = viewModel;
        WindowInteropHelperOwner.TrySetOwner(this, application);
    }

    public ParkingSettings Settings => viewModel.ToSettings();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

internal sealed class ParkingSettingsViewModel
{
    public ParkingSettingsViewModel(double parkingAreaSquareMeters)
    {
        ParkingAreaText = $"Powierzchnia parkingow: {parkingAreaSquareMeters:N2} m2";
        RegularWidthText = "2,5";
        RegularLengthText = "5,0";
        AccessibleCountText = "0";
        AccessibleWidthText = "3,6";
        AccessibleLengthText = "6,0";
    }

    public string ParkingAreaText { get; }

    public string RegularWidthText { get; set; }

    public string RegularLengthText { get; set; }

    public string AccessibleCountText { get; set; }

    public string AccessibleWidthText { get; set; }

    public string AccessibleLengthText { get; set; }

    public ParkingSettings ToSettings()
    {
        return new ParkingSettings(
            ReadDouble(RegularWidthText, 2.5),
            ReadDouble(RegularLengthText, 5.0),
            Math.Max(0, (int)Math.Round(ReadDouble(AccessibleCountText, 0))),
            ReadDouble(AccessibleWidthText, 3.6),
            ReadDouble(AccessibleLengthText, 6.0));
    }

    private static double ReadDouble(string value, double fallback)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out double currentCultureValue))
        {
            return currentCultureValue;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariantValue))
        {
            return invariantValue;
        }

        return fallback;
    }
}
