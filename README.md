# NoRecoil

NoRecoil now includes:

- A **Windows C# desktop app** (`src/NoRecoil.Desktop`) with sliders for Right/Down/Left/Up/Delay
- Preset **save/load** buttons
- Last-selected file path cache
- INI compatibility with both regular key/value format and this structure:

```ini
[Recoil]
Right=0
Down=37
Left=0
Up=0
Delay=0
```

- Recoil control that moves the mouse while LMB is pressed (using `Right-Left` and `Down-Up`)
- Bluetooth serial sync support (COM port)
- A companion **Android app** (`android/NoRecoilBluetoothApp`) that sends preset values over Bluetooth SPP

## Desktop app

Build and run on Windows:

```bash
dotnet run --project src/NoRecoil.Desktop/NoRecoil.Desktop.csproj
```

## Core tests

```bash
dotnet test tests/NoRecoil.Core.Tests/NoRecoil.Core.Tests.csproj
```

## Android app

Open `android/NoRecoilBluetoothApp` in Android Studio and build/run the `app` module.
