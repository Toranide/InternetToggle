# Changelog

All notable changes to InternetToggle are documented here.

## [1.0.0] - 2026-10-28

### Added
- Select a detected network adapter from the system-tray menu.
- Remember the chosen adapter in the current Windows user's AppData folder.
- Toggle the selected adapter with the global shortcut \`Ctrl + Alt + T\`.
- Opt-in startup task management from the tray menu.
- Tray status and balloon notifications for adapter operations.
- README, MIT license, usage guidance, and a Windows/.NET 10 GitHub Actions workflow.

### Changed
- Move administrator elevation to application startup, before creating the form.
- Use Windows PowerShell's \`Get-NetAdapter\`, \`Enable-NetAdapter\`, and \`Disable-NetAdapter\` for adapter state and toggle operations.
- Add command timeouts and clearer error messages.
- Avoid disposing the live tray-menu collection while enumerating it.

### Notes
- InternetToggle enables or disables one network adapter; it is not a firewall and does not guarantee all applications are disconnected from the internet.
- This version has not yet been validated by a successful GitHub-hosted build in the current environment.
