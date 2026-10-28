# Usage

## What it does

InternetToggle is a Windows system-tray helper for enabling or disabling one selected network adapter. It can reduce repeated trips to Windows Settings during focused work.

## Quick start
1. Start the app and approve the Windows administrator prompt.
2. Find the InternetToggle icon in the system tray.
3. Open the tray menu and choose **Select network adapter**.
4. Select the adapter to control.
5. Use the toggle command or press **Ctrl + Alt + T**.
6. Enable **Run at Windows sign-in** only if you want the app to start automatically.

## Important limitation

This application changes the administrative state of a single adapter. Other adapters, VPNs, mobile hotspots, virtual network interfaces, or other routes can still provide internet access. It does not selectively block Premiere Pro or any other application.

The status in the menu shows whether Windows reports the adapter as enabled; it does not test whether internet access is working.

## Permissions and startup

Administrator privileges are required to change adapter state. Automatic startup is optional and can be switched off from the tray menu, which removes the scheduled task created by the app.
