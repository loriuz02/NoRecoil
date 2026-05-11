using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using NoRecoil.Core;

namespace NoRecoil.Desktop;

public partial class Form1 : Form
{
    private readonly LastPathCache _lastPathCache = new();
    private readonly string _profilePath = Path.Combine(AppContext.BaseDirectory, "profile.ini");
    private readonly System.Windows.Forms.Timer _recoilTimer = new();
    private readonly System.Windows.Forms.Timer _hotkeyTimer = new();
    private readonly StringBuilder _bluetoothBuffer = new();
    private SerialPort? _serialPort;
    private bool _applyingProfile;
    private readonly List<RuntimeHotkeyBinding> _hotkeys = [];
    private readonly TabControl _tabs = new();
    private readonly TabPage _mainTab = new("🎯 Main");
    private readonly TabPage _keybindsTab = new("⌨️ Keybinds");
    private readonly TabPage _bluetoothTab = new("📶 Bluetooth");
    private readonly Label _hotkeysLabel = new();
    private readonly Label _keybindHintLabel = new();
    private readonly ListBox _hotkeysList = new();
    private readonly Button _addHotkeyButton = new();
    private readonly Button _editHotkeyButton = new();
    private readonly Button _removeHotkeyButton = new();

    public Form1()
    {
        InitializeComponent();
        InitializeTabbedLayout();
        InitializeKeybindsTab();
        ApplyEmojiText();

        _recoilTimer.Interval = 10;
        _recoilTimer.Tick += RecoilTimerOnTick;
        _hotkeyTimer.Interval = 25;
        _hotkeyTimer.Tick += HotkeyTimerOnTick;
        _hotkeyTimer.Start();

        ApplyPreset(new RecoilPreset());
        ApplyRecoilState(false);
        requireRightMouseCheckbox.Checked = true;
        ApplyModernTheme();

        LoadProfile();

        var lastPath = _lastPathCache.Load();
        if (!string.IsNullOrWhiteSpace(lastPath) && File.Exists(lastPath))
        {
            ApplyPreset(PresetIniSerializer.LoadFromFile(lastPath));
            pathValueLabel.Text = lastPath;
        }

        UpdateDelayLabel();
    }

    private void toggleRecoilButton_Click(object sender, EventArgs e)
    {
        ApplyRecoilState(!_recoilTimer.Enabled);
        SaveProfile();
    }

    private void setEnableHotkeyButton_Click(object? sender, EventArgs e)
    {
        _tabs.SelectedTab = _keybindsTab;
    }

    private void requireRightMouseCheckbox_CheckedChanged(object sender, EventArgs e)
    {
        SaveProfile();
    }

    private void bluetoothPortTextbox_TextChanged(object sender, EventArgs e)
    {
        SaveProfile();
    }

    private void slider_ValueChanged(object sender, EventArgs e)
    {
        rightValueLabel.Text = rightSlider.Value.ToString();
        downValueLabel.Text = downSlider.Value.ToString();
        leftValueLabel.Text = leftSlider.Value.ToString();
        upValueLabel.Text = upSlider.Value.ToString();
        UpdateDelayLabel();

        TrySendPresetToBluetooth();
        SaveProfile();
    }

    private void saveButton_Click(object sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            FileName = "preset.ini",
        };

        SetInitialDirectory(dialog);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        PresetIniSerializer.SaveToFile(dialog.FileName, BuildPreset(), includeSectionHeader: true);
        _lastPathCache.Save(dialog.FileName);
        pathValueLabel.Text = dialog.FileName;
        SaveProfile();
    }

    private void loadButton_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            CheckFileExists = true,
        };

        SetInitialDirectory(dialog);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var preset = PresetIniSerializer.LoadFromFile(dialog.FileName);
        ApplyPreset(preset);

        _lastPathCache.Save(dialog.FileName);
        pathValueLabel.Text = dialog.FileName;

        TrySendPresetToBluetooth();
        SaveProfile();
    }

    private void connectBluetoothButton_Click(object sender, EventArgs e)
    {
        if (_serialPort is not null && _serialPort.IsOpen)
        {
            DisconnectBluetooth();
            return;
        }

        var portName = bluetoothPortTextbox.Text.Trim();
        if (string.IsNullOrWhiteSpace(portName))
        {
            MessageBox.Show(this, "Enter a Bluetooth COM port (example: COM5).", "NoRecoil", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _serialPort = new SerialPort(portName, 9600)
            {
                NewLine = "\n",
            };
            _serialPort.DataReceived += SerialPortOnDataReceived;
            _serialPort.Open();

            bluetoothStatusValueLabel.Text = "🟢 Connected";
            connectBluetoothButton.Text = "🔌 Disconnect";

            TrySendPresetToBluetooth();
            SaveProfile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Bluetooth connection failed: {ex.Message}", "NoRecoil", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DisconnectBluetooth();
        }
    }

    private void RecoilTimerOnTick(object? sender, EventArgs e)
    {
        if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) == 0)
        {
            return;
        }

        if (requireRightMouseCheckbox.Checked && (GetAsyncKeyState(VK_RBUTTON) & 0x8000) == 0)
        {
            return;
        }

        var preset = BuildPreset();
        if (preset.HorizontalDelta == 0 && preset.VerticalDelta == 0)
        {
            return;
        }

        mouse_event(MOUSEEVENTF_MOVE, preset.HorizontalDelta, preset.VerticalDelta, 0, 0);

        var delay = Math.Max(preset.Delay, 0);
        _recoilTimer.Interval = Math.Max(delay, 1);
    }

    private void HotkeyTimerOnTick(object? sender, EventArgs e)
    {
        foreach (var hotkeyBinding in _hotkeys)
        {
            ProcessHotkey(ref hotkeyBinding.IsPressed, hotkeyBinding.Hotkey, onPressed: () =>
            {
                ExecuteHotkeyAction(hotkeyBinding);
            });
        }
    }

    private void SetInitialDirectory(FileDialog dialog)
    {
        var lastPath = _lastPathCache.Load();
        if (string.IsNullOrWhiteSpace(lastPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(lastPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }
    }

    private RecoilPreset BuildPreset() => new()
    {
        Right = rightSlider.Value,
        Down = downSlider.Value,
        Left = leftSlider.Value,
        Up = upSlider.Value,
        Delay = delaySlider.Value,
    };

    private void ApplyPreset(RecoilPreset preset)
    {
        rightSlider.Value = CoerceToRange(preset.Right, rightSlider.Minimum, rightSlider.Maximum);
        downSlider.Value = CoerceToRange(preset.Down, downSlider.Minimum, downSlider.Maximum);
        leftSlider.Value = CoerceToRange(preset.Left, leftSlider.Minimum, leftSlider.Maximum);
        upSlider.Value = CoerceToRange(preset.Up, upSlider.Minimum, upSlider.Maximum);
        delaySlider.Value = CoerceToRange(preset.Delay, delaySlider.Minimum, delaySlider.Maximum);

        rightValueLabel.Text = rightSlider.Value.ToString();
        downValueLabel.Text = downSlider.Value.ToString();
        leftValueLabel.Text = leftSlider.Value.ToString();
        upValueLabel.Text = upSlider.Value.ToString();
        UpdateDelayLabel();
    }

    private static int CoerceToRange(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private void UpdateDelayLabel()
    {
        delayValueLabel.Text = $"{delaySlider.Value} ms";
    }

    private void ApplyRecoilState(bool enabled)
    {
        _recoilTimer.Enabled = enabled;
        statusValueLabel.Text = enabled ? "🟢 Active" : "🔴 Not Active";
        toggleRecoilButton.Text = enabled ? "🛑 Disable recoil" : "🎯 Enable recoil";
    }

    private ProfileSettings BuildProfileSettings() => new()
    {
        Preset = BuildPreset(),
        RecoilEnabled = _recoilTimer.Enabled,
        RequireRightMouseButton = requireRightMouseCheckbox.Checked,
        BluetoothPort = bluetoothPortTextbox.Text.Trim(),
        EnableHotkey = GetRequiredHotkey(HotkeyAction.EnableRecoil, ProfileSettings.DefaultEnableHotkeyKey),
        DisableHotkey = GetRequiredHotkey(HotkeyAction.DisableRecoil, ProfileSettings.DefaultDisableHotkeyKey),
        PresetHotkeys = _hotkeys
            .Where(item => item.Action is not HotkeyAction.EnableRecoil and not HotkeyAction.DisableRecoil)
            .Take(ProfileSettings.MaxPresetHotkeys)
            .Select(item => new PresetHotkeyBinding
            {
                Hotkey = item.Hotkey,
                PresetPath = EncodeHotkeyTarget(item),
            })
            .ToArray(),
    };

    private void ApplyProfileSettings(ProfileSettings settings)
    {
        _applyingProfile = true;
        try
        {
            ApplyPreset(settings.Preset);
            ApplyRecoilState(settings.RecoilEnabled);
            requireRightMouseCheckbox.Checked = settings.RequireRightMouseButton;
            bluetoothPortTextbox.Text = settings.BluetoothPort;
            _hotkeys.Clear();
            _hotkeys.Add(new RuntimeHotkeyBinding
            {
                Hotkey = NormalizeHotkey(settings.EnableHotkey, ProfileSettings.DefaultEnableHotkeyKey),
                Action = HotkeyAction.EnableRecoil,
            });
            _hotkeys.Add(new RuntimeHotkeyBinding
            {
                Hotkey = NormalizeHotkey(settings.DisableHotkey, ProfileSettings.DefaultDisableHotkeyKey),
                Action = HotkeyAction.DisableRecoil,
            });

            foreach (var item in settings.PresetHotkeys.Take(ProfileSettings.MaxPresetHotkeys))
            {
                var decoded = DecodeHotkeyTarget(item.PresetPath);
                if (decoded.Action == HotkeyAction.PresetSelection && string.IsNullOrWhiteSpace(decoded.PresetPath))
                {
                    continue;
                }

                var normalized = NormalizeHotkey(item.Hotkey, fallbackKey: 0);
                if (!IsValidVirtualKey(normalized.Key))
                {
                    continue;
                }

                _hotkeys.Add(new RuntimeHotkeyBinding
                {
                    Hotkey = normalized,
                    Action = decoded.Action,
                    PresetPath = decoded.PresetPath,
                });
            }

            RemoveDuplicateHotkeys();
            RefreshHotkeyList();
        }
        finally
        {
            _applyingProfile = false;
        }
    }

    private void LoadProfile()
    {
        try
        {
            if (!File.Exists(_profilePath))
            {
                SaveProfile();
                return;
            }

            ApplyProfileSettings(ProfileIniSerializer.LoadFromFile(_profilePath));
        }
        catch
        {
            SaveProfile();
        }
    }

    private void SaveProfile()
    {
        if (_applyingProfile)
        {
            return;
        }

        try
        {
            ProfileIniSerializer.SaveToFile(_profilePath, BuildProfileSettings(), includeSectionHeader: true);
        }
        catch
        {
            // ignored: profile persistence should not break runtime behavior
        }
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort is null)
        {
            return;
        }

        try
        {
            var incoming = _serialPort.ReadExisting();
            if (string.IsNullOrEmpty(incoming))
            {
                return;
            }

            lock (_bluetoothBuffer)
            {
                _bluetoothBuffer.Append(incoming);
                ProcessBluetoothBuffer();
            }
        }
        catch
        {
            // ignored: connection can drop while reading
        }
    }

    private void ProcessBluetoothBuffer()
    {
        var payload = _bluetoothBuffer.ToString();
        var chunks = payload.Split("\n\n", StringSplitOptions.None);
        if (chunks.Length <= 1)
        {
            return;
        }

        _bluetoothBuffer.Clear();
        _bluetoothBuffer.Append(chunks[^1]);

        foreach (var chunk in chunks[..^1])
        {
            var content = chunk.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            try
            {
                var preset = PresetIniSerializer.Parse(content);
                BeginInvoke(() => ApplyPreset(preset));
            }
            catch
            {
                // ignore malformed payloads
            }
        }
    }

    private void TrySendPresetToBluetooth()
    {
        if (_serialPort is null || !_serialPort.IsOpen)
        {
            return;
        }

        try
        {
            var payload = PresetIniSerializer.Serialize(BuildPreset(), includeSectionHeader: true) + Environment.NewLine;
            _serialPort.Write(payload);
        }
        catch
        {
            // ignore transient write failures
        }
    }

    private void DisconnectBluetooth()
    {
        if (_serialPort is not null)
        {
            try
            {
                _serialPort.DataReceived -= SerialPortOnDataReceived;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                _serialPort = null;
            }
        }

        connectBluetoothButton.Text = "🔗 Connect";
        bluetoothStatusValueLabel.Text = "🔴 Disconnected";
        SaveProfile();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _hotkeyTimer.Stop();
        DisconnectBluetooth();
        base.OnFormClosing(e);
    }

    private void InitializeTabbedLayout()
    {
        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(20, 6);
        _tabs.Multiline = false;

        _tabs.TabPages.Add(_mainTab);
        _tabs.TabPages.Add(_keybindsTab);
        _tabs.TabPages.Add(_bluetoothTab);
        Controls.Add(_tabs);

        foreach (var control in new Control[]
                 {
                     rightSlider,
                     downSlider,
                     leftSlider,
                     upSlider,
                     delaySlider,
                     rightLabel,
                     downLabel,
                     leftLabel,
                     upLabel,
                     delayLabel,
                     rightValueLabel,
                     downValueLabel,
                     leftValueLabel,
                     upValueLabel,
                     delayValueLabel,
                     saveButton,
                     loadButton,
                     toggleRecoilButton,
                     requireRightMouseCheckbox,
                     statusLabel,
                     statusValueLabel,
                     pathLabel,
                     pathValueLabel,
                 })
        {
            _mainTab.Controls.Add(control);
        }

        foreach (var control in new Control[]
                 {
                     bluetoothPortLabel,
                     bluetoothPortTextbox,
                     connectBluetoothButton,
                     bluetoothStatusLabel,
                     bluetoothStatusValueLabel,
                 })
        {
            _bluetoothTab.Controls.Add(control);
        }

        manageKeybindsButton.Visible = false;
        manageKeybindsButton.Enabled = false;

        bluetoothPortLabel.Location = new Point(20, 30);
        bluetoothPortTextbox.Location = new Point(145, 27);
        connectBluetoothButton.Location = new Point(255, 24);
        bluetoothStatusLabel.Location = new Point(20, 74);
        bluetoothStatusValueLabel.Location = new Point(102, 74);
    }

    private void InitializeKeybindsTab()
    {
        _hotkeysLabel.AutoSize = true;
        _hotkeysLabel.Location = new Point(20, 20);
        _hotkeysLabel.Text = "⌨️ Hotkeys list";

        _keybindHintLabel.Location = new Point(20, 46);
        _keybindHintLabel.Size = new Size(470, 24);
        _keybindHintLabel.Text = "💡 F1/F2 are preconfigured; add or edit hotkeys to customize actions.";

        _hotkeysList.Location = new Point(20, 74);
        _hotkeysList.Size = new Size(470, 194);
        _hotkeysList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _hotkeysList.Format += (_, args) =>
        {
            if (args.ListItem is RuntimeHotkeyBinding item)
            {
                args.Value = $"{FormatHotkey(item.Hotkey)} ➜ {GetActionDisplayText(item)}";
            }
        };

        _addHotkeyButton.Location = new Point(20, 278);
        _addHotkeyButton.Size = new Size(150, 36);
        _addHotkeyButton.Text = "➕ Add hotkey";
        _addHotkeyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _addHotkeyButton.Click += (_, _) => AddHotkey();

        _editHotkeyButton.Location = new Point(180, 278);
        _editHotkeyButton.Size = new Size(150, 36);
        _editHotkeyButton.Text = "✏️ Edit selected";
        _editHotkeyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _editHotkeyButton.Click += (_, _) => EditSelectedHotkey();

        _removeHotkeyButton.Location = new Point(340, 278);
        _removeHotkeyButton.Size = new Size(150, 36);
        _removeHotkeyButton.Text = "➖ Remove selected";
        _removeHotkeyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _removeHotkeyButton.Click += (_, _) => RemoveSelectedHotkey();

        _keybindsTab.Controls.Add(_hotkeysLabel);
        _keybindsTab.Controls.Add(_keybindHintLabel);
        _keybindsTab.Controls.Add(_hotkeysList);
        _keybindsTab.Controls.Add(_addHotkeyButton);
        _keybindsTab.Controls.Add(_editHotkeyButton);
        _keybindsTab.Controls.Add(_removeHotkeyButton);

        EnsureDefaultHotkeys();
        RefreshHotkeyList();
    }

    private void ApplyEmojiText()
    {
        Text = "NoRecoil ✨ Controller";
        saveButton.Text = "💾 Save preset";
        loadButton.Text = "📂 Load preset";
        requireRightMouseCheckbox.Text = "🖱️ Require right mouse together with left";
        statusLabel.Text = "Recoil:";
        pathLabel.Text = "🗂️ Last path:";
        bluetoothPortLabel.Text = "🔌 Bluetooth COM";
        bluetoothPortTextbox.PlaceholderText = "COM5";
        connectBluetoothButton.Text = "🔗 Connect";
        bluetoothStatusLabel.Text = "Status:";
        bluetoothStatusValueLabel.Text = "🔴 Disconnected";
        _hotkeysLabel.Text = "⌨️ Hotkeys list";
    }

    private void RefreshHotkeyList()
    {
        EnsureDefaultHotkeys();
        _hotkeysList.Items.Clear();
        foreach (var hotkeyBinding in _hotkeys.OrderBy(hotkey => hotkey.Hotkey.Modifiers).ThenBy(hotkey => hotkey.Hotkey.Key))
        {
            _hotkeysList.Items.Add(hotkeyBinding);
        }
    }

    private void AddHotkey()
    {
        if (_hotkeys.Count(item => item.Action is not HotkeyAction.EnableRecoil and not HotkeyAction.DisableRecoil)
            >= ProfileSettings.MaxPresetHotkeys)
        {
            MessageBox.Show(
                this,
                $"You can configure up to {ProfileSettings.MaxPresetHotkeys} custom hotkeys.",
                "NoRecoil",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var actionDialog = new HotkeyActionDialog();
        if (actionDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CaptureAndApplyHotkey(
            "Configure hotkey",
            $"⌨️ Press the key combination for {GetActionDisplayName(actionDialog.SelectedAction)}",
            binding => UpsertHotkey(actionDialog.SelectedAction, binding, actionDialog.SelectedPresetPath));
    }

    private void EditSelectedHotkey()
    {
        if (_hotkeysList.SelectedItem is not RuntimeHotkeyBinding selected)
        {
            return;
        }

        CaptureAndApplyHotkey(
            "Change hotkey",
            $"⌨️ Press the new key combination for {GetActionDisplayText(selected)}",
            binding =>
            {
                selected.Hotkey = binding;
                DeduplicateByHotkey(selected);
            });
    }

    private void RemoveSelectedHotkey()
    {
        if (_hotkeysList.SelectedItem is not RuntimeHotkeyBinding selected)
        {
            return;
        }

        if (selected.Action is HotkeyAction.EnableRecoil or HotkeyAction.DisableRecoil)
        {
            MessageBox.Show(this, "Enable/disable hotkeys are required and cannot be removed.", "NoRecoil", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _hotkeys.Remove(selected);
        RefreshHotkeyList();
        SaveProfile();
    }

    private void CaptureAndApplyHotkey(string title, string prompt, Action<HotkeyBinding> apply)
    {
        using var dialog = new HotkeyCaptureDialog(title, prompt);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.CapturedHotkey is null)
        {
            return;
        }

        apply(dialog.CapturedHotkey);
        ResetPressedStates();
        RefreshHotkeyList();
        SaveProfile();
    }

    private void EnsureDefaultHotkeys()
    {
        if (!_hotkeys.Any(item => item.Action == HotkeyAction.EnableRecoil))
        {
            _hotkeys.Add(new RuntimeHotkeyBinding
            {
                Hotkey = new HotkeyBinding { Key = ProfileSettings.DefaultEnableHotkeyKey, Modifiers = HotkeyModifiers.None },
                Action = HotkeyAction.EnableRecoil,
            });
        }

        if (!_hotkeys.Any(item => item.Action == HotkeyAction.DisableRecoil))
        {
            _hotkeys.Add(new RuntimeHotkeyBinding
            {
                Hotkey = new HotkeyBinding { Key = ProfileSettings.DefaultDisableHotkeyKey, Modifiers = HotkeyModifiers.None },
                Action = HotkeyAction.DisableRecoil,
            });
        }
    }

    private HotkeyBinding GetRequiredHotkey(HotkeyAction action, int fallbackKey)
    {
        var match = _hotkeys.FirstOrDefault(item => item.Action == action);
        return NormalizeHotkey(match?.Hotkey, fallbackKey);
    }

    private void UpsertHotkey(HotkeyAction action, HotkeyBinding hotkey, string? presetPath)
    {
        var existingByAction = action is HotkeyAction.EnableRecoil or HotkeyAction.DisableRecoil
            ? _hotkeys.FirstOrDefault(item => item.Action == action)
            : null;
        if (existingByAction is not null)
        {
            existingByAction.Hotkey = hotkey;
            return;
        }

        var existingByHotkey = _hotkeys.FirstOrDefault(
            item => item.Hotkey.Key == hotkey.Key && item.Hotkey.Modifiers == hotkey.Modifiers);
        var normalizedPresetPath = string.IsNullOrWhiteSpace(presetPath) ? string.Empty : presetPath.Trim();
        if (existingByHotkey is null)
        {
            _hotkeys.Add(new RuntimeHotkeyBinding
            {
                Hotkey = hotkey,
                Action = action,
                PresetPath = normalizedPresetPath,
            });
        }
        else
        {
            existingByHotkey.Action = action;
            existingByHotkey.PresetPath = normalizedPresetPath;
        }
    }

    private void RemoveDuplicateHotkeys()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = _hotkeys.Count - 1; i >= 0; i--)
        {
            var current = _hotkeys[i];
            var key = $"{current.Hotkey.Key}:{(int)current.Hotkey.Modifiers}";
            if (seen.Contains(key))
            {
                _hotkeys.RemoveAt(i);
                continue;
            }

            seen.Add(key);
        }
    }

    private void DeduplicateByHotkey(RuntimeHotkeyBinding selected)
    {
        var duplicates = _hotkeys
            .Where(item => !ReferenceEquals(item, selected)
                           && item.Hotkey.Key == selected.Hotkey.Key
                           && item.Hotkey.Modifiers == selected.Hotkey.Modifiers)
            .ToList();
        foreach (var duplicate in duplicates)
        {
            _hotkeys.Remove(duplicate);
        }
    }

    private void ResetPressedStates()
    {
        foreach (var hotkeyBinding in _hotkeys)
        {
            hotkeyBinding.IsPressed = false;
        }
    }

    private void ExecuteHotkeyAction(RuntimeHotkeyBinding hotkeyBinding)
    {
        switch (hotkeyBinding.Action)
        {
            case HotkeyAction.PresetSelection:
                if (string.IsNullOrWhiteSpace(hotkeyBinding.PresetPath) || !File.Exists(hotkeyBinding.PresetPath))
                {
                    return;
                }

                var preset = PresetIniSerializer.LoadFromFile(hotkeyBinding.PresetPath);
                ApplyPreset(preset);
                _lastPathCache.Save(hotkeyBinding.PresetPath);
                pathValueLabel.Text = hotkeyBinding.PresetPath;
                TrySendPresetToBluetooth();
                SaveProfile();
                break;
            case HotkeyAction.EnableRecoil:
                if (!_recoilTimer.Enabled)
                {
                    ApplyRecoilState(true);
                    SaveProfile();
                }

                break;
            case HotkeyAction.DisableRecoil:
                if (_recoilTimer.Enabled)
                {
                    ApplyRecoilState(false);
                    SaveProfile();
                }

                break;
            case HotkeyAction.ToggleRecoil:
                ApplyRecoilState(!_recoilTimer.Enabled);
                SaveProfile();
                break;
            case HotkeyAction.IncreaseDown:
                ApplyDirectionalStep(downSlider, 1);
                break;
            case HotkeyAction.DecreaseDown:
                ApplyDirectionalStep(downSlider, -1);
                break;
            case HotkeyAction.IncreaseUp:
                ApplyDirectionalStep(upSlider, 1);
                break;
            case HotkeyAction.DecreaseUp:
                ApplyDirectionalStep(upSlider, -1);
                break;
            case HotkeyAction.IncreaseRight:
                ApplyDirectionalStep(rightSlider, 1);
                break;
            case HotkeyAction.DecreaseRight:
                ApplyDirectionalStep(rightSlider, -1);
                break;
            case HotkeyAction.IncreaseLeft:
                ApplyDirectionalStep(leftSlider, 1);
                break;
            case HotkeyAction.DecreaseLeft:
                ApplyDirectionalStep(leftSlider, -1);
                break;
        }
    }

    private void ApplyDirectionalStep(TrackBar slider, int step)
    {
        var updated = Math.Clamp(slider.Value + step, slider.Minimum, slider.Maximum);
        if (updated == slider.Value)
        {
            return;
        }

        slider.Value = updated;
        slider_ValueChanged(slider, EventArgs.Empty);
    }

    private static string EncodeHotkeyTarget(RuntimeHotkeyBinding binding)
    {
        return binding.Action == HotkeyAction.PresetSelection
            ? binding.PresetPath
            : $"{ActionTargetPrefix}{binding.Action}";
    }

    private static (HotkeyAction Action, string PresetPath) DecodeHotkeyTarget(string target)
    {
        if (target.StartsWith(ActionTargetPrefix, StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<HotkeyAction>(target[ActionTargetPrefix.Length..], ignoreCase: true, out var action)
            )
        {
            return (action, string.Empty);
        }

        return (HotkeyAction.PresetSelection, target);
    }

    private static string GetActionDisplayText(RuntimeHotkeyBinding item)
    {
        return item.Action == HotkeyAction.PresetSelection
            ? $"Preset: {Path.GetFileName(item.PresetPath)}"
            : GetActionDisplayName(item.Action);
    }

    private static string GetActionDisplayName(HotkeyAction action)
    {
        return action switch
        {
            HotkeyAction.PresetSelection => "Preset selection",
            HotkeyAction.EnableRecoil => "Enable recoil",
            HotkeyAction.DisableRecoil => "Disable recoil",
            HotkeyAction.ToggleRecoil => "Toggle recoil",
            HotkeyAction.IncreaseDown => "Increase down value",
            HotkeyAction.DecreaseDown => "Decrease down value",
            HotkeyAction.IncreaseUp => "Increase up value",
            HotkeyAction.DecreaseUp => "Decrease up value",
            HotkeyAction.IncreaseRight => "Increase right value",
            HotkeyAction.DecreaseRight => "Decrease right value",
            HotkeyAction.IncreaseLeft => "Increase left value",
            HotkeyAction.DecreaseLeft => "Decrease left value",
            _ => action.ToString(),
        };
    }

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int MinSupportedVirtualKey = 0x01; // Lowest Windows virtual-key code used by GetAsyncKeyState.
    private const int MaxSupportedVirtualKey = 0xFE; // Upper bound supported here; 0xFE (VK_PACKET) and 0xFF have special meanings.
    private static readonly Color AccentBorderColor = Color.FromArgb(80, 120, 191);
    private const string ActionTargetPrefix = "__action__:";

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, nuint dwExtraInfo);

    private void ProcessHotkey(ref bool previousState, HotkeyBinding hotkey, Action onPressed)
    {
        if (!IsValidVirtualKey(hotkey.Key))
        {
            previousState = false;
            return;
        }

        var currentlyPressed = IsHotkeyPressed(hotkey);
        if (currentlyPressed && !previousState)
        {
            onPressed();
        }

        previousState = currentlyPressed;
    }

    private static HotkeyBinding NormalizeHotkey(HotkeyBinding? hotkey, int fallbackKey)
    {
        if (hotkey is null)
        {
            return new HotkeyBinding { Key = fallbackKey, Modifiers = HotkeyModifiers.None };
        }

        var key = IsValidVirtualKey(hotkey.Key) ? hotkey.Key : fallbackKey;
        return new HotkeyBinding
        {
            Key = key,
            Modifiers = hotkey.Modifiers,
        };
    }

    private static bool IsHotkeyPressed(HotkeyBinding hotkey)
    {
        if (!IsValidVirtualKey(hotkey.Key))
        {
            return false;
        }

        var keyPressed = IsVirtualKeyPressed(hotkey.Key);
        if (!keyPressed)
        {
            return false;
        }

        var shiftPressed = IsVirtualKeyPressed(VK_SHIFT);
        var controlPressed = IsVirtualKeyPressed(VK_CONTROL);
        var altPressed = IsVirtualKeyPressed(VK_MENU);

        var requiresShift = hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift);
        var requiresControl = hotkey.Modifiers.HasFlag(HotkeyModifiers.Control);
        var requiresAlt = hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt);

        return shiftPressed == requiresShift
               && controlPressed == requiresControl
               && altPressed == requiresAlt;
    }

    private static bool IsValidVirtualKey(int value) => value >= MinSupportedVirtualKey && value <= MaxSupportedVirtualKey;

    private static bool IsVirtualKeyPressed(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    private static HotkeyBinding? BuildHotkeyBinding(KeyEventArgs e)
    {
        var key = (int)e.KeyCode;
        if (key is (int)Keys.ShiftKey or (int)Keys.ControlKey or (int)Keys.Menu)
        {
            return null;
        }

        if (!IsValidVirtualKey(key))
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

        _tabs.BackColor = Color.FromArgb(24, 29, 40);
        _tabs.ForeColor = Color.Gainsboro;

        ApplyThemeRecursive(this);
        _hotkeysList.BackColor = Color.FromArgb(31, 38, 55);
        _hotkeysList.ForeColor = Color.Gainsboro;
        foreach (TabPage page in _tabs.TabPages)
        {
            page.BackColor = Color.FromArgb(20, 24, 33);
            page.ForeColor = Color.Gainsboro;
        }
    }

    private static void ApplyThemeRecursive(Control root)
    {
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = AccentBorderColor;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 52, 76);
                    button.BackColor = Color.FromArgb(31, 38, 55);
                    button.ForeColor = Color.Gainsboro;
                    break;
                case TextBox textBox:
                    textBox.BackColor = Color.FromArgb(31, 38, 55);
                    textBox.ForeColor = Color.Gainsboro;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case Label label:
                    label.ForeColor = Color.Gainsboro;
                    break;
                case CheckBox checkBox:
                    checkBox.ForeColor = Color.Gainsboro;
                    break;
            }

            ApplyThemeRecursive(control);
        }
    }

    private sealed class RuntimeHotkeyBinding
    {
        public HotkeyBinding Hotkey { get; set; } = new();
        public HotkeyAction Action { get; set; } = HotkeyAction.PresetSelection;
        public string PresetPath { get; set; } = string.Empty;
        public bool IsPressed;
    }

    private enum HotkeyAction
    {
        PresetSelection,
        EnableRecoil,
        DisableRecoil,
        ToggleRecoil,
        IncreaseDown,
        DecreaseDown,
        IncreaseUp,
        DecreaseUp,
        IncreaseRight,
        DecreaseRight,
        IncreaseLeft,
        DecreaseLeft,
    }

    private sealed class HotkeyActionDialog : Form
    {
        private sealed record ActionOption(HotkeyAction Action, string Label);
        private readonly List<ActionOption> _actionOptions = Enum.GetValues<HotkeyAction>()
            .Select(action => new ActionOption(action, GetActionDisplayName(action)))
            .ToList();
        private readonly ComboBox _actionCombo = new();
        private readonly TextBox _presetPathTextBox = new();
        private readonly Button _browseButton = new();
        private readonly Label _presetPathLabel = new();

        public HotkeyAction SelectedAction => (_actionCombo.SelectedItem as ActionOption)?.Action ?? HotkeyAction.PresetSelection;
        public string SelectedPresetPath => _presetPathTextBox.Text.Trim();

        public HotkeyActionDialog()
        {
            Text = "Select hotkey action";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 200);
            BackColor = Color.FromArgb(20, 24, 33);
            ForeColor = Color.Gainsboro;

            var actionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(16, 20),
                Text = "Action",
                ForeColor = Color.Gainsboro,
            };
            _actionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _actionCombo.Location = new Point(16, 45);
            _actionCombo.Size = new Size(468, 28);
            _actionCombo.DisplayMember = nameof(ActionOption.Label);
            _actionCombo.Items.AddRange(_actionOptions.Cast<object>().ToArray());
            _actionCombo.SelectedItem = _actionOptions[0];
            _actionCombo.SelectedIndexChanged += (_, _) => TogglePresetPathInput();

            _presetPathLabel.AutoSize = true;
            _presetPathLabel.Location = new Point(16, 86);
            _presetPathLabel.Text = "Preset file";
            _presetPathLabel.ForeColor = Color.Gainsboro;

            _presetPathTextBox.Location = new Point(16, 111);
            _presetPathTextBox.Size = new Size(364, 27);
            _presetPathTextBox.BackColor = Color.FromArgb(31, 38, 55);
            _presetPathTextBox.ForeColor = Color.Gainsboro;
            _presetPathTextBox.BorderStyle = BorderStyle.FixedSingle;

            _browseButton.Location = new Point(388, 108);
            _browseButton.Size = new Size(96, 32);
            _browseButton.Text = "Browse";
            _browseButton.FlatStyle = FlatStyle.Flat;
            _browseButton.BackColor = Color.FromArgb(31, 38, 55);
            _browseButton.ForeColor = Color.Gainsboro;
            _browseButton.FlatAppearance.BorderColor = AccentBorderColor;
            _browseButton.Click += (_, _) => BrowsePresetPath();

            var saveButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(298, 156),
                Size = new Size(90, 32),
                Text = "Continue",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(31, 38, 55),
                ForeColor = Color.Gainsboro,
            };
            saveButton.FlatAppearance.BorderColor = AccentBorderColor;
            saveButton.Click += (_, e) =>
            {
                if (SelectedAction == HotkeyAction.PresetSelection
                    && (string.IsNullOrWhiteSpace(SelectedPresetPath) || !File.Exists(SelectedPresetPath)))
                {
                    MessageBox.Show(this, "Select a valid preset INI file for preset selection.", "NoRecoil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                }
            };

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(394, 156),
                Size = new Size(90, 32),
                Text = "Cancel",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(31, 38, 55),
                ForeColor = Color.Gainsboro,
            };
            cancelButton.FlatAppearance.BorderColor = AccentBorderColor;

            Controls.Add(actionLabel);
            Controls.Add(_actionCombo);
            Controls.Add(_presetPathLabel);
            Controls.Add(_presetPathTextBox);
            Controls.Add(_browseButton);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            TogglePresetPathInput();
        }

        private void TogglePresetPathInput()
        {
            var isPresetSelection = SelectedAction == HotkeyAction.PresetSelection;
            _presetPathLabel.Visible = isPresetSelection;
            _presetPathTextBox.Visible = isPresetSelection;
            _browseButton.Visible = isPresetSelection;
        }

        private void BrowsePresetPath()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                DefaultExt = "ini",
                CheckFileExists = true,
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _presetPathTextBox.Text = dialog.FileName;
            }
        }
    }

    private sealed class HotkeyCaptureDialog : Form
    {
        public HotkeyBinding? CapturedHotkey { get; private set; }

        public HotkeyCaptureDialog(string title, string prompt)
        {
            Text = title;
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 150);
            BackColor = Color.FromArgb(20, 24, 33);
            ForeColor = Color.Gainsboro;

            var promptLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 18),
                Size = new Size(408, 48),
                Text = prompt,
                ForeColor = Color.Gainsboro,
            };

            var waitingLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 68),
                Size = new Size(408, 24),
                Text = "🕒 Waiting for key combination...",
                ForeColor = Color.LightSkyBlue,
            };

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(316, 104),
                Size = new Size(108, 30),
                Text = "Cancel",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(31, 38, 55),
                ForeColor = Color.Gainsboro,
            };
            cancelButton.FlatAppearance.BorderColor = AccentBorderColor;

            Controls.Add(promptLabel);
            Controls.Add(waitingLabel);
            Controls.Add(cancelButton);
            CancelButton = cancelButton;

            KeyDown += OnCaptureKeyDown;
        }

        private void OnCaptureKeyDown(object? sender, KeyEventArgs e)
        {
            var binding = BuildHotkeyBinding(e);
            if (binding is null)
            {
                return;
            }

            CapturedHotkey = binding;
            e.SuppressKeyPress = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
