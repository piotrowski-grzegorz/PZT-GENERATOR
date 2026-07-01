using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Autodesk.Revit.UI;

namespace PztGenerator;

public partial class AssignPztPresetWindow : Window
{
    private readonly AssignPztPresetViewModel viewModel;

    public AssignPztPresetWindow(int selectedAreaCount, UIApplication application)
    {
        InitializeComponent();
        viewModel = new AssignPztPresetViewModel(selectedAreaCount);
        DataContext = viewModel;
        WindowInteropHelperOwner.TrySetOwner(this, application);
    }

    public PztPreset? SelectedPreset => viewModel.SelectedPreset;

    private void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

internal sealed class AssignPztPresetViewModel : INotifyPropertyChanged
{
    private PztPreset? selectedPreset;

    public AssignPztPresetViewModel(int selectedAreaCount)
    {
        SelectionText = $"Zaznaczone obszary: {selectedAreaCount}";
        Presets = PztPreset.All;
        selectedPreset = Presets.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SelectionText { get; }

    public IReadOnlyList<PztPreset> Presets { get; }

    public PztPreset? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            selectedPreset = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PresetDescription));
        }
    }

    public string PresetDescription => SelectedPreset is null
        ? string.Empty
        : $"Kategoria: {SelectedPreset.Category}, stan: {FormatState(SelectedPreset.Status)}";

    private static string FormatState(string state)
    {
        return string.IsNullOrWhiteSpace(state) ? "-" : state;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
