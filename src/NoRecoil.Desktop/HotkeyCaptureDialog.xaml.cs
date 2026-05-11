using System.Windows;
using System.Windows.Input;
using NoRecoil.Core;

namespace NoRecoil.Desktop;

public partial class HotkeyCaptureDialog : Window
{
    public HotkeyBinding? CapturedHotkey { get; private set; }

    public HotkeyCaptureDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var binding = MainWindow.BuildHotkeyBinding(e);
        if (binding is null)
        {
            return;
        }

        CapturedHotkey = binding;
        e.Handled = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
