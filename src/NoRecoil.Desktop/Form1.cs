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
    private readonly StringBuilder _bluetoothBuffer = new();
    private SerialPort? _serialPort;
    private bool _applyingProfile;

    public Form1()
    {
        InitializeComponent();

        _recoilTimer.Interval = 10;
        _recoilTimer.Tick += RecoilTimerOnTick;

        ApplyPreset(new RecoilPreset());
        ApplyRecoilState(false);
        requireRightMouseCheckbox.Checked = true;

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

    private void disableRecoilButton_Click(object sender, EventArgs e)
    {
        ApplyRecoilState(false);
        SaveProfile();
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
        DisconnectBluetooth();
        base.OnFormClosing(e);
    }

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, nuint dwExtraInfo);
}
