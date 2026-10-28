# Contributing

Thanks for considering a contribution to InternetToggle.

## Development requirements
- Windows 10 or Windows 11.
- .NET 10 SDK.
- Visual Studio 2022 or a compatible .NET command-line environment.

## Build locally

\`\`\`powershell
dotnet restore InternetToggle.slnx
dotnet build InternetToggle.slnx --configuration Release
\`\`\`

## Before opening a pull request
- Keep the tray workflow quick and simple.
- Explain user-visible behavior changes in the pull request.
- Avoid adding dependencies unless there is a clear benefit.
- Do not commit generated output, local settings, credentials, or personal machine details.
- Test adapter selection, enable/disable behavior, startup task management, and the Ctrl+Alt+T shortcut on Windows.

## Reporting issues
Use GitHub Issues and include the Windows version, .NET SDK/runtime details, steps to reproduce, and any relevant error message. Do not include credentials or sensitive machine information.
