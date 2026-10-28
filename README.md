# InternetToggle

A small Windows system-tray utility for quickly enabling or disabling a chosen network adapter. It was made for workflows where you need to cut a particular connection temporarily without opening Windows Settings each time.

## Features

- Toggle the selected network adapter from the system tray.
- Use the global shortcut **Ctrl + Alt + T**.
- Select between the network adapters detected by Windows.
- See the selected adapter's administrative state in the tray menu and icon.
- Optionally start at Windows sign-in.
- Remember the selected adapter between launches.
- No third-party NuGet packages.

## Important: what this app changes

InternetToggle enables or disables **one Windows network adapter**. It does not block all internet traffic. If another adapter, VPN, tethered phone, or virtual network interface has a working route, apps may still have internet access.

The status shown by the app is the adapter's administrative state, not a test that websites or the internet are reachable.

## Requirements

- Windows 10 or Windows 11.
- Administrator privileges to enable or disable adapters.
- .NET 10 SDK to build from source.

The app requests administrator access because Windows requires it to change adapter state. Automatic startup is **off by default** and can be enabled from the tray menu.

## Use

1. Launch InternetToggle. Accept the Windows administrator prompt.
2. Find the app icon in the system tray near the clock.
3. Right-click the icon and select **Select network adapter** to choose the adapter you want to control.
4. Use **Enable** or **Disable**, or press **Ctrl + Alt + T**.
5. To start the app automatically at sign-in, choose **Run at Windows sign-in** in the tray menu. Choose it again to remove the scheduled task.

If the shortcut does not work, another application may already have registered Ctrl + Alt + T.

## Build

From the repository root:

    dotnet restore InternetToggle.slnx
    dotnet build InternetToggle.slnx --configuration Release

To publish a self-contained, single-file build for 64-bit Windows:

    dotnet publish InternetToggle/InternetToggle.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output artifacts/win-x64

Run the generated InternetToggle.exe from the publish folder. Build outputs are ignored by Git and should not be committed to the repository.

## GitHub Actions and releases

Pushes and pull requests build the project and attach a downloadable InternetToggle-win-x64 artifact to the workflow run. To create a versioned GitHub Release with the ready-to-run executable, push a version tag after confirming the build works:

    git tag v1.0.0
    git push origin v1.0.0

The release workflow builds the self-contained 64-bit Windows executable and attaches it to the new GitHub Release.

## License

Released under the MIT License. See LICENSE.
