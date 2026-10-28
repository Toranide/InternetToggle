# Changelog

All notable changes to InternetToggle are documented here.

## [1.0.0] - 2026-10-28

### Added
- Select a detected network adapter from the system-tray menu.
- Remember the chosen adapter in the current Windows user's AppData folder.
- Toggle the selected adapter with the global shortcut \`Ctrl + Alt + T\`.
- Separate enable and disable commands that reflect the adapter's current state.
- A custom borderless dark tray popup with rounded corners, vector-drawn icons, adapter selection, and clearer enable/disable actions.
- Opt-in startup task management from the tray menu.
- Tray status and balloon notifications for adapter operations.
- Project cover artwork, README, usage guide, contribution guide, changelog, MIT license, and Windows/.NET 10 build/release workflow.

### Changed
- Move administrator elevation to application startup, before creating the form.
- Use Windows PowerShell's \`Get-NetAdapter\`, \`Enable-NetAdapter\`, and \`Disable-NetAdapter\` for adapter state and toggle operations.
- Add command timeouts and clearer error messages.
- Avoid disposing the live tray-menu collection while enumerating it.
- Replace the generated SVG cover in the README with the original uploaded JPEG project poster.

### Notes
- InternetToggle enables or disables one network adapter; it is not a firewall and does not guarantee all applications are disconnected from the internet.
- The displayed state reports whether Windows considers the adapter administratively enabled; it does not verify that internet access is available.
- The current code and interface changes still require a successful build and smoke test on Windows before declaring the release validated.
