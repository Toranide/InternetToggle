using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace InternetToggle;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        if (!IsAdministrator())
        {
            try
            {
                string? processPath = Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(processPath))
                {
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                // Also supports starting the app with "dotnet run" while developing.
                if (Path.GetFileNameWithoutExtension(processPath)
                    .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyPath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(assemblyPath))
                    {
                        startInfo.Arguments = $"\"{assemblyPath}\"";
                    }
                }

                Process.Start(startInfo);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // The user cancelled the UAC prompt.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "InternetToggle could not request administrator access.\n\n" + ex.Message,
                    "InternetToggle",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
