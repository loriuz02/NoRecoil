using System.ComponentModel;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NoRecoil.Core;

namespace NoRecoil.Desktop;

internal sealed class ViGEmGamepadOutput : IDisposable
{
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;
    private bool _connected;

    public void Connect()
    {
        _client ??= new ViGEmClient();
        _controller ??= _client.CreateXbox360Controller();
        _controller.AutoSubmitReport = false;
        _controller.Connect();
        _connected = true;
        Center();
    }

    public void Move(RecoilPreset preset)
    {
        var position = GamepadStickPosition.FromPreset(preset);
        var controller = _controller ?? throw new InvalidOperationException("The virtual controller is not connected.");
        controller.SetAxisValue(Xbox360Axis.RightThumbX, position.X);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, position.Y);
        controller.SubmitReport();
    }

    public void Center()
    {
        if (_connected && _controller is not null)
        {
            _controller.ResetReport();
            _controller.SubmitReport();
        }
    }

    public void Dispose()
    {
        var controller = _controller;
        var client = _client;
        try
        {
            Center();
        }
        finally
        {
            _controller = null;
            _client = null;
            _connected = false;
            try
            {
                controller?.Disconnect();
            }
            finally
            {
                try
                {
                    (controller as IDisposable)?.Dispose();
                }
                finally
                {
                    client?.Dispose();
                }
            }
        }
    }

    public static bool IsDriverError(Exception exception)
    {
        return exception is Win32Exception or DllNotFoundException or EntryPointNotFoundException
            or BadImageFormatException or SEHException
            || exception.GetType().Namespace == typeof(VigemBusNotFoundException).Namespace
            || exception is TypeInitializationException { InnerException: { } inner } && IsDriverError(inner);
    }
}
