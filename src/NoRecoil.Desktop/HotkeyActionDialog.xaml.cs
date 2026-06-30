using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace NoRecoil.Desktop;

public partial class HotkeyActionDialog : Window
{
    private sealed record ActionOption(MainWindow.HotkeyAction Action, string Label);

    private readonly List<ActionOption> _actionOptions = Enum.GetValues<MainWindow.HotkeyAction>()
        .Select(action => new ActionOption(action, GetActionDisplayName(action)))
        .ToList();

    internal MainWindow.HotkeyAction SelectedAction => (ActionComboBox.SelectedItem as ActionOption)?.Action ?? MainWindow.HotkeyAction.PresetSelection;
    internal string SelectedPresetPath => PresetPathTextBox.Text.Trim();

    public HotkeyActionDialog()
    {
        InitializeComponent();

        ActionComboBox.ItemsSource = _actionOptions;
        ActionComboBox.SelectedIndex = 0;
        TogglePresetPathInput();
    }

    private void ActionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        TogglePresetPathInput();
    }

    private void TogglePresetPathInput()
    {
        var isPresetSelection = SelectedAction == MainWindow.HotkeyAction.PresetSelection;
        var visibility = isPresetSelection ? Visibility.Visible : Visibility.Collapsed;
        PresetPathLabel.Visibility = visibility;
        PresetPathTextBox.Visibility = visibility;
        BrowsePresetButton.Visibility = visibility;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == true)
        {
            PresetPathTextBox.Text = dialog.FileName;
        }
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAction == MainWindow.HotkeyAction.PresetSelection
            && (string.IsNullOrWhiteSpace(SelectedPresetPath) || !File.Exists(SelectedPresetPath)))
        {
            MessageBox.Show(this, "Select a valid preset INI file for preset selection.", "NoRecoil", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string GetActionDisplayName(MainWindow.HotkeyAction action)
    {
        return action switch
        {
            MainWindow.HotkeyAction.PresetSelection => "Preset selection",
            MainWindow.HotkeyAction.EnableRecoil => "Enable / Primary recoil",
            MainWindow.HotkeyAction.DisableRecoil => "Disable / Secondary recoil",
            MainWindow.HotkeyAction.ForceDisableRecoil => "Force disable recoil",
            MainWindow.HotkeyAction.ToggleRecoil => "Toggle recoil",
            MainWindow.HotkeyAction.IncreaseDown => "Increase down value",
            MainWindow.HotkeyAction.DecreaseDown => "Decrease down value",
            MainWindow.HotkeyAction.IncreaseUp => "Increase up value",
            MainWindow.HotkeyAction.DecreaseUp => "Decrease up value",
            MainWindow.HotkeyAction.IncreaseRight => "Increase right value",
            MainWindow.HotkeyAction.DecreaseRight => "Decrease right value",
            MainWindow.HotkeyAction.IncreaseLeft => "Increase left value",
            MainWindow.HotkeyAction.DecreaseLeft => "Decrease left value",
            _ => action.ToString(),
        };
    }
}
