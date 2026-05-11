using NoRecoil.Core;

namespace NoRecoil.Desktop;

public sealed class KeybindsForm : Form
{
    private readonly List<PresetHotkeyBinding> _presetHotkeys;
    private readonly ListBox _presetHotkeysList = new();
    private readonly Label _captureHintLabel = new();
    private PendingCapture _pendingCapture = PendingCapture.None;
    private string _pendingPresetPath = string.Empty;

    public HotkeyBinding EnableHotkey { get; private set; }
    public HotkeyBinding DisableHotkey { get; private set; }
    public IReadOnlyList<PresetHotkeyBinding> PresetHotkeys => _presetHotkeys;

    public KeybindsForm(HotkeyBinding enableHotkey, HotkeyBinding disableHotkey, IEnumerable<PresetHotkeyBinding> presetHotkeys)
    {
        EnableHotkey = enableHotkey;
        DisableHotkey = disableHotkey;
        _presetHotkeys = presetHotkeys.Select(item => new PresetHotkeyBinding
        {
            Hotkey = item.Hotkey,
            PresetPath = item.PresetPath,
        }).ToList();

        KeyPreview = true;
        Text = "Keybinds";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(660, 420);

        var enableButton = new Button
        {
            Name = "enableButton",
            Location = new Point(20, 20),
            Size = new Size(290, 36),
        };
        enableButton.Click += (_, _) => BeginCapture(PendingCapture.Enable);
        Controls.Add(enableButton);

        var disableButton = new Button
        {
            Name = "disableButton",
            Location = new Point(330, 20),
            Size = new Size(290, 36),
        };
        disableButton.Click += (_, _) => BeginCapture(PendingCapture.Disable);
        Controls.Add(disableButton);

        _captureHintLabel.AutoSize = false;
        _captureHintLabel.Location = new Point(20, 66);
        _captureHintLabel.Size = new Size(600, 24);
        Controls.Add(_captureHintLabel);

        var presetHotkeysLabel = new Label
        {
            AutoSize = true,
            Location = new Point(20, 102),
            Text = "Preset hotkeys",
        };
        Controls.Add(presetHotkeysLabel);

        _presetHotkeysList.Location = new Point(20, 130);
        _presetHotkeysList.Size = new Size(600, 210);
        _presetHotkeysList.FormattingEnabled = true;
        _presetHotkeysList.Format += (_, args) =>
        {
            if (args.ListItem is PresetHotkeyBinding item)
            {
                var fileName = Path.GetFileName(item.PresetPath);
                args.Value = $"{FormatHotkey(item.Hotkey)} -> {fileName}";
            }
        };
        Controls.Add(_presetHotkeysList);

        var addButton = new Button
        {
            Location = new Point(20, 350),
            Size = new Size(190, 36),
            Text = "Add preset hotkey",
        };
        addButton.Click += (_, _) => AddPresetHotkey();
        Controls.Add(addButton);

        var removeButton = new Button
        {
            Location = new Point(220, 350),
            Size = new Size(190, 36),
            Text = "Remove selected",
        };
        removeButton.Click += (_, _) => RemoveSelectedHotkey();
        Controls.Add(removeButton);

        var saveButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(430, 350),
            Size = new Size(90, 36),
            Text = "Save",
        };
        Controls.Add(saveButton);

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(530, 350),
            Size = new Size(90, 36),
            Text = "Cancel",
        };
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        KeyDown += OnKeyDownCapture;

        UpdateButtonLabels();
        RefreshHotkeyList();
        ApplyModernTheme();
    }

    private void AddPresetHotkey()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _pendingPresetPath = dialog.FileName;
        BeginCapture(PendingCapture.AddPreset);
    }

    private void RemoveSelectedHotkey()
    {
        if (_presetHotkeysList.SelectedItem is not PresetHotkeyBinding selected)
        {
            return;
        }

        _presetHotkeys.Remove(selected);
        RefreshHotkeyList();
    }

    private void BeginCapture(PendingCapture pendingCapture)
    {
        _pendingCapture = pendingCapture;
        _captureHintLabel.Text = pendingCapture switch
        {
            PendingCapture.Enable => "Press the hotkey combination for enabling recoil...",
            PendingCapture.Disable => "Press the hotkey combination for disabling recoil...",
            PendingCapture.AddPreset => "Press the hotkey combination for loading the selected preset...",
            _ => string.Empty,
        };
    }

    private void OnKeyDownCapture(object? sender, KeyEventArgs e)
    {
        if (_pendingCapture == PendingCapture.None)
        {
            return;
        }

        var binding = BuildHotkeyBinding(e);
        if (binding is null)
        {
            return;
        }

        switch (_pendingCapture)
        {
            case PendingCapture.Enable:
                EnableHotkey = binding;
                break;
            case PendingCapture.Disable:
                DisableHotkey = binding;
                break;
            case PendingCapture.AddPreset:
                var existingBinding = _presetHotkeys.FirstOrDefault(item => item.Hotkey.Key == binding.Key && item.Hotkey.Modifiers == binding.Modifiers);
                var updated = new PresetHotkeyBinding
                {
                    Hotkey = binding,
                    PresetPath = _pendingPresetPath,
                };

                if (existingBinding is null)
                {
                    _presetHotkeys.Add(updated);
                }
                else
                {
                    var index = _presetHotkeys.IndexOf(existingBinding);
                    _presetHotkeys[index] = updated;
                }

                break;
        }

        _pendingCapture = PendingCapture.None;
        _pendingPresetPath = string.Empty;
        _captureHintLabel.Text = string.Empty;
        UpdateButtonLabels();
        RefreshHotkeyList();
        e.SuppressKeyPress = true;
    }

    private void UpdateButtonLabels()
    {
        var enableButton = Controls.Find("enableButton", searchAllChildren: false).FirstOrDefault() as Button;
        var disableButton = Controls.Find("disableButton", searchAllChildren: false).FirstOrDefault() as Button;
        if (enableButton is not null)
        {
            enableButton.Text = _pendingCapture == PendingCapture.Enable
                ? "Press hotkey..."
                : $"Set enable hotkey ({FormatHotkey(EnableHotkey)})";
        }

        if (disableButton is not null)
        {
            disableButton.Text = _pendingCapture == PendingCapture.Disable
                ? "Press hotkey..."
                : $"Set disable hotkey ({FormatHotkey(DisableHotkey)})";
        }
    }

    private void RefreshHotkeyList()
    {
        _presetHotkeysList.Items.Clear();
        foreach (var item in _presetHotkeys.OrderBy(item => item.Hotkey.Modifiers).ThenBy(item => item.Hotkey.Key))
        {
            _presetHotkeysList.Items.Add(item);
        }
    }

    private static HotkeyBinding? BuildHotkeyBinding(KeyEventArgs e)
    {
        var key = (int)e.KeyCode;
        if (key is (int)Keys.ShiftKey or (int)Keys.ControlKey or (int)Keys.Menu)
        {
            return null;
        }

        if (key < 1 || key > 0xFE)
        {
            return null;
        }

        var modifiers = HotkeyModifiers.None;
        if (e.Control)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (e.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (e.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        return new HotkeyBinding
        {
            Key = key,
            Modifiers = modifiers,
        };
    }

    private static string FormatHotkey(HotkeyBinding hotkey)
    {
        var parts = new List<string>();
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(((Keys)hotkey.Key).ToString());
        return string.Join("+", parts);
    }

    private void ApplyModernTheme()
    {
        BackColor = Color.FromArgb(20, 24, 33);
        ForeColor = Color.Gainsboro;
        foreach (var button in Controls.OfType<Button>())
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(64, 95, 152);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 52, 76);
            button.BackColor = Color.FromArgb(31, 38, 55);
            button.ForeColor = Color.Gainsboro;
        }

        foreach (var label in Controls.OfType<Label>())
        {
            label.ForeColor = Color.Gainsboro;
        }

        _presetHotkeysList.BackColor = Color.FromArgb(31, 38, 55);
        _presetHotkeysList.ForeColor = Color.Gainsboro;
    }

    private enum PendingCapture
    {
        None,
        Enable,
        Disable,
        AddPreset,
    }
}
