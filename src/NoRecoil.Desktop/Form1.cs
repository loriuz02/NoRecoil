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
    private HotkeyBinding _enableHotkey = new() { Key = ProfileSettings.DefaultEnableHotkeyKey };
    private HotkeyBinding _disableHotkey = new() { Key = ProfileSettings.DefaultDisableHotkeyKey };
    private readonly List<RuntimePresetHotkey> _presetHotkeys = [];
    private bool _enableHotkeyPressed;
    private bool _disableHotkeyPressed;

    public Form1()
    {
        InitializeComponent();

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

    private void setEnableHotkeyButton_Click(object sender, EventArgs e)
    {
        OpenKeybindManager();
    }

    private void setDisableHotkeyButton_Click(object sender, EventArgs e)
    {
        OpenKeybindManager();
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

            bluetoothStatusValueLabel.Text = "Connected";
            connectBluetoothButton.Text = "Disconnect";

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
        ProcessHotkey(ref _enableHotkeyPressed, _enableHotkey, onPressed: () =>
        {
            if (!_recoilTimer.Enabled)
            {
                ApplyRecoilState(true);
                SaveProfile();
            }
        });

        ProcessHotkey(ref _disableHotkeyPressed, _disableHotkey, onPressed: () =>
        {
            if (_recoilTimer.Enabled)
            {
                ApplyRecoilState(false);
                SaveProfile();
            }
        });

        foreach (var presetHotkey in _presetHotkeys)
        {
            ProcessHotkey(ref presetHotkey.IsPressed, presetHotkey.Hotkey, onPressed: () =>
            {
                if (!File.Exists(presetHotkey.PresetPath))
                {
                    return;
                }

                var preset = PresetIniSerializer.LoadFromFile(presetHotkey.PresetPath);
                ApplyPreset(preset);
                _lastPathCache.Save(presetHotkey.PresetPath);
                pathValueLabel.Text = presetHotkey.PresetPath;
                TrySendPresetToBluetooth();
                SaveProfile();
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
        statusValueLabel.Text = enabled ? "Enabled" : "Disabled";
        toggleRecoilButton.Text = enabled ? "Toggle recoil (disable)" : "Toggle recoil (enable)";
    }

    private ProfileSettings BuildProfileSettings() => new()
    {
        Preset = BuildPreset(),
        RecoilEnabled = _recoilTimer.Enabled,
        RequireRightMouseButton = requireRightMouseCheckbox.Checked,
        BluetoothPort = bluetoothPortTextbox.Text.Trim(),
        EnableHotkey = _enableHotkey,
        DisableHotkey = _disableHotkey,
        PresetHotkeys = _presetHotkeys
            .Select(item => new PresetHotkeyBinding
            {
                Hotkey = item.Hotkey,
                PresetPath = item.PresetPath,
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
            _enableHotkey = NormalizeHotkey(settings.EnableHotkey, ProfileSettings.DefaultEnableHotkeyKey);
            _disableHotkey = NormalizeHotkey(settings.DisableHotkey, ProfileSettings.DefaultDisableHotkeyKey);
            _presetHotkeys.Clear();
            foreach (var item in settings.PresetHotkeys)
            {
                if (string.IsNullOrWhiteSpace(item.PresetPath))
                {
                    continue;
                }

                var normalized = NormalizeHotkey(item.Hotkey, fallbackKey: 0);
                if (!IsValidVirtualKey(normalized.Key))
                {
                    continue;
                }

                _presetHotkeys.Add(new RuntimePresetHotkey
                {
                    Hotkey = normalized,
                    PresetPath = item.PresetPath,
                });
            }
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

        connectBluetoothButton.Text = "Connect";
        bluetoothStatusValueLabel.Text = "Disconnected";
        SaveProfile();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _hotkeyTimer.Stop();
        DisconnectBluetooth();
        base.OnFormClosing(e);
    }

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int MinSupportedVirtualKey = 0x01; // Lowest Windows virtual-key code used by GetAsyncKeyState.
    private const int MaxSupportedVirtualKey = 0xFE; // Upper bound supported here; 0xFE (VK_PACKET) and 0xFF have special meanings.

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, nuint dwExtraInfo);

    private void OpenKeybindManager()
    {
        using var keybindsForm = new KeybindsForm(_enableHotkey, _disableHotkey, _presetHotkeys
            .Select(item => new PresetHotkeyBinding
            {
                Hotkey = item.Hotkey,
                PresetPath = item.PresetPath,
            }));

        if (keybindsForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _enableHotkey = NormalizeHotkey(keybindsForm.EnableHotkey, ProfileSettings.DefaultEnableHotkeyKey);
        _disableHotkey = NormalizeHotkey(keybindsForm.DisableHotkey, ProfileSettings.DefaultDisableHotkeyKey);
        _enableHotkeyPressed = false;
        _disableHotkeyPressed = false;
        _presetHotkeys.Clear();
        _presetHotkeys.AddRange(keybindsForm.PresetHotkeys.Select(item => new RuntimePresetHotkey
        {
            Hotkey = NormalizeHotkey(item.Hotkey, fallbackKey: 0),
            PresetPath = item.PresetPath,
        }).Where(item => IsValidVirtualKey(item.Hotkey.Key)));
        SaveProfile();
    }

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

        foreach (var textBox in Controls.OfType<TextBox>())
        {
            textBox.BackColor = Color.FromArgb(31, 38, 55);
            textBox.ForeColor = Color.Gainsboro;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (var label in Controls.OfType<Label>())
        {
            label.ForeColor = Color.Gainsboro;
        }
    }

    private sealed class RuntimePresetHotkey
    {
        public HotkeyBinding Hotkey { get; init; } = new();
        public string PresetPath { get; init; } = string.Empty;
        public bool IsPressed { get; set; }
    }
}
