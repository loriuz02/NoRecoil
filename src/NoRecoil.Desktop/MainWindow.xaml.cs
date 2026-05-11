using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NoRecoil.Core;

namespace NoRecoil.Desktop;

public partial class MainWindow : Window
{
    private readonly LastPathCache _lastPathCache = new();
    private readonly string _profilePath = Path.Combine(AppContext.BaseDirectory, "profile.ini");
    private readonly DispatcherTimer _recoilTimer = new();
    private readonly DispatcherTimer _hotkeyTimer = new();
    private readonly StringBuilder _bluetoothBuffer = new();
    private readonly List<RuntimeHotkeyBinding> _hotkeys = [];
    private SerialPort? _serialPort;
    private bool _applyingProfile;

    public MainWindow()
    {
        InitializeComponent();

        _recoilTimer.Interval = TimeSpan.FromMilliseconds(10);
        _recoilTimer.Tick += RecoilTimerOnTick;

        _hotkeyTimer.Interval = TimeSpan.FromMilliseconds(25);
        _hotkeyTimer.Tick += HotkeyTimerOnTick;
        _hotkeyTimer.Start();

        ApplyPreset(new RecoilPreset());
        ApplyRecoilState(false);
        RequireRightMouseCheckBox.IsChecked = true;
        BluetoothPortTextBox.Text = "COM5";

        EnsureDefaultHotkeys();
        RefreshHotkeyList();
        LoadProfile();

        var lastPath = _lastPathCache.Load();
        if (!string.IsNullOrWhiteSpace(lastPath) && File.Exists(lastPath))
        {
            ApplyPreset(PresetIniSerializer.LoadFromFile(lastPath));
            LastPathText.Text = lastPath;
        }

        UpdateDelayLabel();
        SetBluetoothState(isConnected: false);
    }

    private void ToggleRecoilButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyRecoilState(!_recoilTimer.IsEnabled);
        SaveProfile();
    }

    private void RequireRightMouseCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        SaveProfile();
    }

    private void BluetoothPortTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SaveProfile();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        RightValueText.Text = RightSlider.Value.ToString("0");
        DownValueText.Text = DownSlider.Value.ToString("0");
        LeftValueText.Text = LeftSlider.Value.ToString("0");
        UpValueText.Text = UpSlider.Value.ToString("0");
        UpdateDelayLabel();

        TrySendPresetToBluetooth();
        SaveProfile();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            FileName = "preset.ini",
        };

        SetInitialDirectory(dialog);
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        PresetIniSerializer.SaveToFile(dialog.FileName, BuildPreset(), includeSectionHeader: true);
        _lastPathCache.Save(dialog.FileName);
        LastPathText.Text = dialog.FileName;
        SaveProfile();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            DefaultExt = "ini",
            CheckFileExists = true,
        };

        SetInitialDirectory(dialog);
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var preset = PresetIniSerializer.LoadFromFile(dialog.FileName);
        ApplyPreset(preset);

        _lastPathCache.Save(dialog.FileName);
        LastPathText.Text = dialog.FileName;

        TrySendPresetToBluetooth();
        SaveProfile();
    }

    private void ConnectBluetoothButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serialPort is not null && _serialPort.IsOpen)
        {
            DisconnectBluetooth();
            return;
        }

        var portName = BluetoothPortTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(portName))
        {
            MessageBox.Show(this, "Enter a Bluetooth COM port (example: COM5).", "NoRecoil", MessageBoxButton.OK, MessageBoxImage.Information);
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

            SetBluetoothState(isConnected: true);
            TrySendPresetToBluetooth();
            SaveProfile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Bluetooth connection failed: {ex.Message}", "NoRecoil", MessageBoxButton.OK, MessageBoxImage.Error);
            DisconnectBluetooth();
        }
    }

    private void RecoilTimerOnTick(object? sender, EventArgs e)
    {
        if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) == 0)
        {
            return;
        }

        if (RequireRightMouseCheckBox.IsChecked == true && (GetAsyncKeyState(VK_RBUTTON) & 0x8000) == 0)
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
        _recoilTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(delay, 1));
    }

    private void HotkeyTimerOnTick(object? sender, EventArgs e)
    {
        foreach (var hotkeyBinding in _hotkeys)
        {
            ProcessHotkey(ref hotkeyBinding.IsPressed, hotkeyBinding.Hotkey, () => ExecuteHotkeyAction(hotkeyBinding));
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
        Right = (int)RightSlider.Value,
        Down = (int)DownSlider.Value,
        Left = (int)LeftSlider.Value,
        Up = (int)UpSlider.Value,
        Delay = (int)DelaySlider.Value,
    };

    private void ApplyPreset(RecoilPreset preset)
    {
        RightSlider.Value = CoerceToRange(preset.Right, (int)RightSlider.Minimum, (int)RightSlider.Maximum);
        DownSlider.Value = CoerceToRange(preset.Down, (int)DownSlider.Minimum, (int)DownSlider.Maximum);
        LeftSlider.Value = CoerceToRange(preset.Left, (int)LeftSlider.Minimum, (int)LeftSlider.Maximum);
        UpSlider.Value = CoerceToRange(preset.Up, (int)UpSlider.Minimum, (int)UpSlider.Maximum);
        DelaySlider.Value = CoerceToRange(preset.Delay, (int)DelaySlider.Minimum, (int)DelaySlider.Maximum);

        RightValueText.Text = RightSlider.Value.ToString("0");
        DownValueText.Text = DownSlider.Value.ToString("0");
        LeftValueText.Text = LeftSlider.Value.ToString("0");
        UpValueText.Text = UpSlider.Value.ToString("0");
        UpdateDelayLabel();
    }

    private static int CoerceToRange(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private void UpdateDelayLabel()
    {
        DelayValueText.Text = $"{DelaySlider.Value:0} ms";
    }

    private void ApplyRecoilState(bool enabled)
    {
        _recoilTimer.IsEnabled = enabled;
        RecoilStatusText.Text = enabled ? "Active" : "Not active";
        RecoilStatusText.Foreground = (Brush)FindResource(enabled ? "SuccessBrush" : "DangerBrush");
        ToggleRecoilButton.Content = enabled ? "Disable recoil" : "Enable recoil";
    }

    private ProfileSettings BuildProfileSettings() => new()
    {
        Preset = BuildPreset(),
        RecoilEnabled = _recoilTimer.IsEnabled,
        RequireRightMouseButton = RequireRightMouseCheckBox.IsChecked == true,
        BluetoothPort = BluetoothPortTextBox.Text.Trim(),
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
            RequireRightMouseCheckBox.IsChecked = settings.RequireRightMouseButton;
            BluetoothPortTextBox.Text = settings.BluetoothPort;

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
                Dispatcher.BeginInvoke(() => ApplyPreset(preset));
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

        SetBluetoothState(isConnected: false);
        SaveProfile();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hotkeyTimer.Stop();
        DisconnectBluetooth();
        base.OnClosed(e);
    }

    private void SetBluetoothState(bool isConnected)
    {
        BluetoothStatusText.Text = isConnected ? "Connected" : "Disconnected";
        BluetoothStatusText.Foreground = (Brush)FindResource(isConnected ? "SuccessBrush" : "DangerBrush");
        ConnectBluetoothButton.Content = isConnected ? "Disconnect" : "Connect";
    }

    private void RefreshHotkeyList()
    {
        EnsureDefaultHotkeys();
        HotkeysListBox.ItemsSource = _hotkeys
            .OrderBy(hotkey => hotkey.Hotkey.Modifiers)
            .ThenBy(hotkey => hotkey.Hotkey.Key)
            .ToList();
    }

    private void AddHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hotkeys.Count(item => item.Action is not HotkeyAction.EnableRecoil and not HotkeyAction.DisableRecoil)
            >= ProfileSettings.MaxPresetHotkeys)
        {
            MessageBox.Show(this, $"You can configure up to {ProfileSettings.MaxPresetHotkeys} custom hotkeys.", "NoRecoil", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var actionDialog = new HotkeyActionDialog { Owner = this };
        if (actionDialog.ShowDialog() != true)
        {
            return;
        }

        CaptureAndApplyHotkey($"Press the key combination for {GetActionDisplayName(actionDialog.SelectedAction)}", binding =>
        {
            UpsertHotkey(actionDialog.SelectedAction, binding, actionDialog.SelectedPresetPath);
        });
    }

    private void EditHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (HotkeysListBox.SelectedItem is not RuntimeHotkeyBinding selected)
        {
            return;
        }

        CaptureAndApplyHotkey($"Press the new key combination for {GetActionDisplayText(selected)}", binding =>
        {
            selected.Hotkey = binding;
            DeduplicateByHotkey(selected);
        });
    }

    private void RemoveHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (HotkeysListBox.SelectedItem is not RuntimeHotkeyBinding selected)
        {
            return;
        }

        if (selected.Action is HotkeyAction.EnableRecoil or HotkeyAction.DisableRecoil)
        {
            MessageBox.Show(this, "Enable/disable hotkeys are required and cannot be removed.", "NoRecoil", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _hotkeys.Remove(selected);
        RefreshHotkeyList();
        SaveProfile();
    }

    private void CaptureAndApplyHotkey(string prompt, Action<HotkeyBinding> apply)
    {
        var dialog = new HotkeyCaptureDialog("Capture hotkey", prompt) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.CapturedHotkey is null)
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

        var existingByHotkey = _hotkeys.FirstOrDefault(item => item.Hotkey.Key == hotkey.Key && item.Hotkey.Modifiers == hotkey.Modifiers);
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
                LastPathText.Text = hotkeyBinding.PresetPath;
                TrySendPresetToBluetooth();
                SaveProfile();
                break;
            case HotkeyAction.EnableRecoil:
                if (!_recoilTimer.IsEnabled)
                {
                    ApplyRecoilState(true);
                    SaveProfile();
                }

                break;
            case HotkeyAction.DisableRecoil:
                if (_recoilTimer.IsEnabled)
                {
                    ApplyRecoilState(false);
                    SaveProfile();
                }

                break;
            case HotkeyAction.ToggleRecoil:
                ApplyRecoilState(!_recoilTimer.IsEnabled);
                SaveProfile();
                break;
            case HotkeyAction.IncreaseDown:
                ApplyDirectionalStep(DownSlider, 1);
                break;
            case HotkeyAction.DecreaseDown:
                ApplyDirectionalStep(DownSlider, -1);
                break;
            case HotkeyAction.IncreaseUp:
                ApplyDirectionalStep(UpSlider, 1);
                break;
            case HotkeyAction.DecreaseUp:
                ApplyDirectionalStep(UpSlider, -1);
                break;
            case HotkeyAction.IncreaseRight:
                ApplyDirectionalStep(RightSlider, 1);
                break;
            case HotkeyAction.DecreaseRight:
                ApplyDirectionalStep(RightSlider, -1);
                break;
            case HotkeyAction.IncreaseLeft:
                ApplyDirectionalStep(LeftSlider, 1);
                break;
            case HotkeyAction.DecreaseLeft:
                ApplyDirectionalStep(LeftSlider, -1);
                break;
        }
    }

    private void ApplyDirectionalStep(Slider slider, int step)
    {
        var updated = Math.Clamp((int)slider.Value + step, (int)slider.Minimum, (int)slider.Maximum);
        if (updated == slider.Value)
        {
            return;
        }

        slider.Value = updated;
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
            && Enum.TryParse<HotkeyAction>(target[ActionTargetPrefix.Length..], true, out var action))
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
    private const int MinSupportedVirtualKey = 0x01;
    private const int MaxSupportedVirtualKey = 0xFE;
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

        if (!IsVirtualKeyPressed(hotkey.Key))
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

    internal static HotkeyBinding? BuildHotkeyBinding(KeyEventArgs e)
    {
        var key = KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
        if (key is VK_SHIFT or VK_CONTROL or VK_MENU)
        {
            return null;
        }

        if (!IsValidVirtualKey(key))
        {
            return null;
        }

        var modifiers = HotkeyModifiers.None;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        return new HotkeyBinding
        {
            Key = key,
            Modifiers = modifiers,
        };
    }

    internal static string FormatHotkey(HotkeyBinding hotkey)
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

        parts.Add(KeyInterop.KeyFromVirtualKey(hotkey.Key).ToString());
        return string.Join("+", parts);
    }

    internal enum HotkeyAction
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

    internal sealed class RuntimeHotkeyBinding
    {
        public HotkeyBinding Hotkey { get; set; } = new();
        public HotkeyAction Action { get; set; } = HotkeyAction.PresetSelection;
        public string PresetPath { get; set; } = string.Empty;
        public bool IsPressed;
        public string DisplayText => $"{FormatHotkey(Hotkey)} ➜ {GetActionDisplayText(this)}";
    }
}
