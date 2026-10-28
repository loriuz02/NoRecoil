# NoRecoil

NoRecoil now includes:

- A **Windows WPF desktop app** (`src/NoRecoil.Desktop`) with sliders for Right/Down/Left/Up/Delay
- Preset **save/load** buttons
- Last-selected file path cache
- Auto-created `profile.ini` in the executable folder to persist selected settings (recoil values, state, trigger mode, Bluetooth COM)
- INI compatibility with both regular key/value format and this structure:

```ini
[Recoil]
Right=0
Down=37
Left=0
Up=0
Delay=0
```

- Recoil control buttons (toggle + disable)
- Optional **secondary preset** system: enable "Switch to secondary preset when toggled off" to load a second recoil, switch between "Recoil 1"/"Recoil 2" with the toggle, and save both in one `[Recoil]`/`[Secondary]` INI file
- Trigger mode switch: default **LMB + RMB** required, or **LMB only**
- Recoil control that moves the mouse while configured trigger is pressed (using `Right-Left` and `Down-Up`)
- Optional **ViGEmBus virtual Xbox 360 output**, selectable independently of the default mouse output
- Bluetooth serial sync support (COM port)
- A companion **Android app** (`android/NoRecoilBluetoothApp`) that sends preset values over Bluetooth SPP

## Desktop app

Build and run on Windows:

```bash
dotnet run --project src/NoRecoil.Desktop/NoRecoil.Desktop.csproj
```

Publish a Windows `.exe`:

```bash
dotnet publish src/NoRecoil.Desktop/NoRecoil.Desktop.csproj -c Release
```

### Optional virtual-controller output

Install the production-signed Windows 10/11 driver from the official
[ViGEmBus project](https://github.com/nefarius/ViGEmBus), then select
**Virtual Xbox 360 controller (ViGEmBus)** under **Output backend** and enable recoil.
The app does not install drivers or change Windows security settings. ViGEmBus is
retired; review its end-of-life notice before installing it.

Mouse output remains the default. Changing the backend disables recoil; the choice
is saved as `OutputBackend=Mouse` or `OutputBackend=ViGEmXbox360` in `profile.ini`.
The virtual controller connects when recoil is enabled and disconnects when the
backend changes or the app closes. Driver failures disable recoil and display an
error, without falling back to mouse output. Enable recoil again to retry.

In virtual-controller mode, Right/Left and Up/Down control the **right stick**:
each net value of 1 represents 1% deflection, with values clamped at 100%.
Down maps to negative Y, Up to positive Y. This is a held stick position, not
mouse displacement; existing mouse presets will need retuning, and game deadzones,
sensitivity and inverted-look settings affect the result. Delay controls the report
interval (minimum 1 ms); trigger release centers the stick on the next timer tick.
Disabling recoil centers it immediately.

The existing LMB / LMB+RMB trigger settings and hotkeys still apply. The app does
**not** forward keyboard, mouse, or physical-controller input into the virtual pad,
and does not send gamepad fire/aim buttons. Games must accept controller aiming
alongside your other inputs; games that switch exclusively between input types
may not work with this setup. ViGEmBus is not a mouse driver, and neither its
kernel implementation nor its signing guarantees anti-cheat compatibility or
undetectability.

On Windows, verify the virtual pad using `joy.cpl`: enable recoil with nonzero
values, hold the configured mouse buttons, and check right-stick movement.
Releasing the trigger or disabling recoil should center the stick; changing back
to mouse output or closing the app should remove the virtual pad. Check
missing-driver behavior before using the integration in another application.

## Core tests

```bash
dotnet test tests/NoRecoil.Core.Tests/NoRecoil.Core.Tests.csproj
```

## Android app

Open `android/NoRecoilBluetoothApp` in Android Studio and build/run the `app` module.
