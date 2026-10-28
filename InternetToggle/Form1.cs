using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace InternetToggle;

public partial class Form1 : Form
{
    private const int HotkeyId = 1;
    private const int ModControl = 0x0002;
    private const int ModAlt = 0x0001;
    private const int VirtualKeyT = 0x54;
    private const string StartupTaskName = "InternetToggleAutoRun";

    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly List<NetworkAdapterInfo> _adapters = [];
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "InternetToggle",
        "settings.json");

    private string? _selectedAdapterName;
    private Icon? _ownedTrayIcon;
    private bool? _lastTrayIconState;
    private bool _hotkeyRegistered;
    private bool _isBusy;
    private bool _adapterQueryFailed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public Form1()
    {
        InitializeComponent();

        _refreshTimer = new System.Windows.Forms.Timer(components)
        {
            Interval = 15_000
        };

        ConfigureTrayMenuAppearance();
        LoadSettings();

        notifyIcon1.DoubleClick += async (_, _) => await ToggleSelectedAdapterAsync();
        enableMenuItem.Click += async (_, _) => await SetSelectedAdapterEnabledAsync(true);
        disableMenuItem.Click += async (_, _) => await SetSelectedAdapterEnabledAsync(false);
        adaptersMenuItem.DropDownOpening += async (_, _) =>
        {
            await RefreshAdaptersAsync(showErrors: false);
            BuildAdapterMenu();
        };
        startupMenuItem.Click += async (_, _) => await ToggleStartupAsync();
        aboutMenuItem.Click += (_, _) => ShowAbout();
        exitMenuItem.Click += (_, _) => Close();

        contextMenuStrip1.Opening += async (_, _) =>
            await RefreshAdaptersAsync(showErrors: false);

        _refreshTimer.Tick += async (_, _) =>
            await RefreshAdaptersAsync(showErrors: false);

        FormClosing += (_, _) =>
        {
            _refreshTimer.Stop();

            if (_hotkeyRegistered)
            {
                UnregisterHotKey(Handle, HotkeyId);
                _hotkeyRegistered = false;
            }

            notifyIcon1.Visible = false;
        };

        FormClosed += (_, _) =>
        {
            _ownedTrayIcon?.Dispose();
            _ownedTrayIcon = null;
        };

        notifyIcon1.Text = "InternetToggle - Starting";
        UpdatePresentation();
    }

    private static readonly Color MenuBackground = Color.FromArgb(11, 23, 39);
    private static readonly Color MenuForeground = Color.FromArgb(232, 242, 255);
    private static readonly Color MenuMuted = Color.FromArgb(139, 166, 197);
    private static readonly Color MenuAccent = Color.FromArgb(77, 189, 255);
    private static readonly Color MenuSuccess = Color.FromArgb(66, 220, 160);
    private static readonly Color MenuWarning = Color.FromArgb(255, 190, 104);
    private static readonly Font TrayMenuFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    private static readonly Font TrayMenuBoldFont = new("Segoe UI", 9.5F, FontStyle.Bold);

    private void ConfigureTrayMenuAppearance()
    {
        var renderer = new TrayMenuRenderer();

        contextMenuStrip1.Renderer = renderer;
        adaptersMenuItem.DropDown.Renderer = renderer;
        contextMenuStrip1.BackColor = MenuBackground;
        contextMenuStrip1.ForeColor = MenuForeground;
        contextMenuStrip1.Padding = new Padding(5);
        contextMenuStrip1.ShowImageMargin = false;
        contextMenuStrip1.ShowCheckMargin = true;

        adaptersMenuItem.DropDown.BackColor = MenuBackground;
        adaptersMenuItem.DropDown.ForeColor = MenuForeground;
        adaptersMenuItem.DropDown.Padding = new Padding(5);
        adaptersMenuItem.DropDown.ShowImageMargin = false;
        adaptersMenuItem.DropDown.ShowCheckMargin = true;

        foreach (ToolStripItem item in contextMenuStrip1.Items)
        {
            if (item is ToolStripSeparator separator)
            {
                separator.Margin = new Padding(8, 4, 8, 4);
                separator.Size = new Size(280, 1);
                continue;
            }

            StyleMenuItem(item);

            if (item == statusMenuItem)
            {
                item.Font = TrayMenuBoldFont;
                item.Size = new Size(310, 38);
            }
        }
    }

    private static void StyleMenuItem(ToolStripItem item)
    {
        item.BackColor = MenuBackground;
        item.ForeColor = MenuForeground;
        item.Font = TrayMenuFont;
        item.Margin = new Padding(2, 1, 2, 1);
        item.Padding = new Padding(10, 5, 10, 5);

        if (item is ToolStripMenuItem menuItem)
        {
            menuItem.AutoSize = false;
            menuItem.Size = new Size(310, 36);
        }
    }

    private sealed class TrayMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => MenuBackground;
        public override Color MenuBorder => Color.FromArgb(47, 76, 108);
        public override Color MenuItemBorder => Color.FromArgb(49, 112, 171);
        public override Color MenuItemSelected => Color.FromArgb(24, 59, 95);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(24, 59, 95);
        public override Color MenuItemSelectedGradientMiddle => Color.FromArgb(24, 59, 95);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(24, 59, 95);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(29, 70, 111);
        public override Color MenuItemPressedGradientMiddle => Color.FromArgb(29, 70, 111);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(29, 70, 111);
        public override Color ImageMarginGradientBegin => MenuBackground;
        public override Color ImageMarginGradientMiddle => MenuBackground;
        public override Color ImageMarginGradientEnd => MenuBackground;
        public override Color SeparatorDark => Color.FromArgb(43, 66, 90);
        public override Color SeparatorLight => Color.FromArgb(43, 66, 90);
        public override Color ToolStripBorder => Color.FromArgb(47, 76, 108);
        public override Color ToolStripGradientBegin => MenuBackground;
        public override Color ToolStripGradientMiddle => MenuBackground;
        public override Color ToolStripGradientEnd => MenuBackground;
    }

    private sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
    {
        public TrayMenuRenderer() : base(new TrayMenuColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.ForeColor;
            base.OnRenderItemText(e);
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        Visible = false;
        ShowInTaskbar = false;
        base.OnLoad(e);

        _hotkeyRegistered = RegisterHotKey(
            Handle,
            HotkeyId,
            ModControl | ModAlt,
            VirtualKeyT);

        if (!_hotkeyRegistered)
        {
            notifyIcon1.ShowBalloonTip(
                3000,
                "InternetToggle",
                "Ctrl+Alt+T is unavailable because another application may already use it.",
                ToolTipIcon.Warning);
        }

        await RefreshAdaptersAsync(showErrors: true);
        await RefreshStartupStateAsync();
        _refreshTimer.Start();
    }

    protected override void WndProc(ref Message m)
    {
        const int WmHotkey = 0x0312;

        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            _ = ToggleSelectedAdapterAsync();
        }

        base.WndProc(ref m);
    }

    private async Task RefreshAdaptersAsync(bool showErrors)
    {
        await _refreshLock.WaitAsync();

        try
        {
            List<NetworkAdapterInfo> refreshedAdapters = await ReadNetworkAdaptersAsync();

            _adapters.Clear();
            _adapters.AddRange(refreshedAdapters);
            _adapterQueryFailed = false;

            if (string.IsNullOrWhiteSpace(_selectedAdapterName))
            {
                NetworkAdapterInfo? preferredAdapter =
                    _adapters.FirstOrDefault(a =>
                        a.Name.StartsWith("Ethernet", StringComparison.OrdinalIgnoreCase))
                    ?? _adapters.FirstOrDefault(a =>
                        a.Name.Equals("Wi-Fi", StringComparison.OrdinalIgnoreCase))
                    ?? _adapters.FirstOrDefault(a =>
                        a.IsEnabled && a.Status.Equals("Up", StringComparison.OrdinalIgnoreCase))
                    ?? _adapters.FirstOrDefault();

                _selectedAdapterName = preferredAdapter?.Name;

                if (_selectedAdapterName is not null)
                {
                    SaveSettings(showErrors: false);
                }
            }

            UpdatePresentation();
        }
        catch (Exception ex)
        {
            _adapters.Clear();
            _adapterQueryFailed = true;
            UpdatePresentation();

            if (showErrors)
            {
                MessageBox.Show(
                    this,
                    "InternetToggle could not read the network adapters.\n\n" + ex.Message +
                    "\n\nMake sure Windows PowerShell and the NetAdapter module are available.",
                    "InternetToggle",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<List<NetworkAdapterInfo>> ReadNetworkAdaptersAsync()
    {
        const string script = """
            $ErrorActionPreference = 'Stop'
            try {
                $items = @(Get-NetAdapter | Select-Object Name, InterfaceDescription, Status, @{
                    Name = 'AdminStatus'
                    Expression = { $_.AdminStatus.ToString() }
                })
                ConvertTo-Json -InputObject $items -Compress -Depth 3
                exit 0
            }
            catch {
                [Console]::Error.WriteLine($_.Exception.Message)
                exit 1
            }
            """;

        ProcessResult result = await RunPowerShellAsync(script, TimeSpan.FromSeconds(15));

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(GetProcessError(result));
        }

        using JsonDocument document = JsonDocument.Parse(result.Output);
        JsonElement root = document.RootElement;
        IEnumerable<JsonElement> items = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().ToArray(),
            JsonValueKind.Object => [root],
            _ => []
        };

        return items
            .Select(item => new NetworkAdapterInfo(
                ReadJsonString(item, "Name"),
                ReadJsonString(item, "InterfaceDescription"),
                ReadJsonString(item, "AdminStatus"),
                ReadJsonString(item, "Status")))
            .Where(adapter => !string.IsNullOrWhiteSpace(adapter.Name))
            .OrderBy(adapter => adapter.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ToString();
    }

    private async Task ToggleSelectedAdapterAsync()
    {
        NetworkAdapterInfo? adapter = GetSelectedAdapter();

        if (adapter is null)
        {
            notifyIcon1.ShowBalloonTip(
                2500,
                "InternetToggle",
                "No network adapter is selected or available. Choose one from the tray menu.",
                ToolTipIcon.Warning);
            return;
        }

        await SetSelectedAdapterEnabledAsync(!adapter.IsEnabled);
    }

    private async Task SetSelectedAdapterEnabledAsync(bool shouldEnable)
    {
        if (_isBusy)
        {
            return;
        }

        NetworkAdapterInfo? adapter = GetSelectedAdapter();

        if (adapter is null)
        {
            notifyIcon1.ShowBalloonTip(
                2500,
                "InternetToggle",
                "No network adapter is selected or available. Choose one from the tray menu.",
                ToolTipIcon.Warning);
            return;
        }

        if (adapter.IsEnabled == shouldEnable)
        {
            notifyIcon1.ShowBalloonTip(
                1800,
                "InternetToggle",
                $"{adapter.Name} is already {(shouldEnable ? "enabled" : "disabled")}.",
                ToolTipIcon.Info);
            return;
        }

        _isBusy = true;
        _refreshTimer.Stop();
        UpdatePresentation();

        try
        {
            string verb = shouldEnable ? "Enable-NetAdapter" : "Disable-NetAdapter";
            string adapterName = ToPowerShellString(adapter.Name);
            string script = string.Join(
                Environment.NewLine,
                new[]
                {
                    "$ErrorActionPreference = 'Stop'",
                    "try {",
                    $"    {verb} -Name {adapterName} -Confirm:$false -ErrorAction Stop | Out-Null",
                    "    Write-Output 'OK'",
                    "    exit 0",
                    "}",
                    "catch {",
                    "    [Console]::Error.WriteLine($_.Exception.Message)",
                    "    exit 1",
                    "}"
                });

            ProcessResult result = await RunPowerShellAsync(script, TimeSpan.FromSeconds(25));

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(GetProcessError(result));
            }

            await Task.Delay(500);
            await RefreshAdaptersAsync(showErrors: false);

            NetworkAdapterInfo? updatedAdapter = GetSelectedAdapter();

            if (updatedAdapter is not null && updatedAdapter.IsEnabled != shouldEnable)
            {
                await Task.Delay(800);
                await RefreshAdaptersAsync(showErrors: false);
                updatedAdapter = GetSelectedAdapter();
            }

            if (updatedAdapter is null || updatedAdapter.IsEnabled != shouldEnable)
            {
                notifyIcon1.ShowBalloonTip(
                    3000,
                    "InternetToggle",
                    "The command completed, but Windows has not reported the expected adapter state yet.",
                    ToolTipIcon.Warning);
            }
            else
            {
                notifyIcon1.ShowBalloonTip(
                    1800,
                    "InternetToggle",
                    $"{updatedAdapter.Name} is now {(shouldEnable ? "enabled" : "disabled")}.",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "The selected network adapter could not be changed.\n\n" + ex.Message,
                "InternetToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            await RefreshAdaptersAsync(showErrors: false);
        }
        finally
        {
            _isBusy = false;
            _refreshTimer.Start();
            UpdatePresentation();
        }
    }

    private void BuildAdapterMenu()
    {
        // Dispose removes items from their owner collection, so never enumerate
        // DropDownItems while disposing its live entries.
        ToolStripItem[] previousItems = adaptersMenuItem.DropDownItems
            .Cast<ToolStripItem>()
            .ToArray();

        adaptersMenuItem.DropDownItems.Clear();

        foreach (ToolStripItem item in previousItems)
        {
            item.Dispose();
        }

        if (_adapters.Count == 0)
        {
            adaptersMenuItem.DropDownItems.Add(new ToolStripMenuItem(
                _adapterQueryFailed ? "Could not load adapters" : "No network adapters found")
            {
                Enabled = false
            });
            return;
        }

        foreach (NetworkAdapterInfo adapter in _adapters)
        {
            string state = adapter.IsEnabled ? "enabled" : "disabled";
            string adapterName = adapter.Name;

            var item = new ToolStripMenuItem($"{adapter.Name}  ·  {state}")
            {
                Checked = string.Equals(
                    adapterName,
                    _selectedAdapterName,
                    StringComparison.OrdinalIgnoreCase),
                CheckOnClick = false,
                ToolTipText = adapter.Description
            };

            StyleMenuItem(item);
            item.Size = new Size(295, 36);
            item.Click += (_, _) => SelectAdapter(adapterName);
            adaptersMenuItem.DropDownItems.Add(item);
        }
    }

    private void SelectAdapter(string adapterName)
    {
        _selectedAdapterName = adapterName;
        SaveSettings();
        UpdatePresentation();
        BuildAdapterMenu();

        notifyIcon1.ShowBalloonTip(
            1800,
            "InternetToggle",
            $"Selected adapter: {adapterName}",
            ToolTipIcon.Info);
    }

    private async Task RefreshStartupStateAsync()
    {
        try
        {
            ProcessResult result = await RunProcessAsync(
                GetSchtasksPath(),
                ["/Query", "/TN", StartupTaskName],
                TimeSpan.FromSeconds(8));

            startupMenuItem.Checked = result.ExitCode == 0;
        }
        catch
        {
            startupMenuItem.Checked = false;
        }

        UpdateStartupMenuText();
    }

    private async Task ToggleStartupAsync()
    {
        bool enableStartup = !startupMenuItem.Checked;
        startupMenuItem.Enabled = false;

        try
        {
            string[] arguments;

            if (enableStartup)
            {
                string taskCommand = GetStartupTaskCommand();
                arguments =
                [
                    "/Create",
                    "/TN", StartupTaskName,
                    "/TR", taskCommand,
                    "/SC", "ONLOGON",
                    "/RL", "HIGHEST",
                    "/F"
                ];
            }
            else
            {
                arguments = ["/Delete", "/TN", StartupTaskName, "/F"];
            }

            ProcessResult result = await RunProcessAsync(
                GetSchtasksPath(),
                arguments,
                TimeSpan.FromSeconds(15));

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(GetProcessError(result));
            }

            startupMenuItem.Checked = enableStartup;
            UpdateStartupMenuText();

            notifyIcon1.ShowBalloonTip(
                2200,
                "InternetToggle",
                enableStartup
                    ? "InternetToggle will run at your next Windows sign-in."
                    : "Automatic startup has been disabled.",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "The startup setting could not be changed.\n\n" + ex.Message,
                "InternetToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            await RefreshStartupStateAsync();
        }
        finally
        {
            startupMenuItem.Enabled = true;
        }
    }

    private void UpdateStartupMenuText()
    {
        startupMenuItem.Text = "⚙  Run at Windows sign-in";
    }

    private static string GetStartupTaskCommand()
    {
        string executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The executable path could not be determined.");

        if (Path.GetFileNameWithoutExtension(executablePath)
            .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            string assemblyPath = typeof(Form1).Assembly.Location;

            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                return $"\"{executablePath}\" \"{assemblyPath}\"";
            }
        }

        return $"\"{executablePath}\"";
    }

    private void UpdatePresentation()
    {
        NetworkAdapterInfo? adapter = GetSelectedAdapter();

        if (_adapterQueryFailed)
        {
            statusMenuItem.Text = "●  NETWORK STATUS  ·  UNAVAILABLE";
            statusMenuItem.ForeColor = MenuWarning;
        }
        else if (_adapters.Count == 0)
        {
            statusMenuItem.Text = "●  NO NETWORK ADAPTERS FOUND";
            statusMenuItem.ForeColor = MenuWarning;
        }
        else if (_selectedAdapterName is null)
        {
            statusMenuItem.Text = "●  CHOOSE A NETWORK ADAPTER";
            statusMenuItem.ForeColor = MenuMuted;
        }
        else if (adapter is null)
        {
            statusMenuItem.Text = $"●  ADAPTER UNAVAILABLE  ·  {_selectedAdapterName}";
            statusMenuItem.ForeColor = MenuWarning;
        }
        else
        {
            statusMenuItem.Text = $"●  {adapter.Name}  ·  {(adapter.IsEnabled ? "ENABLED" : "DISABLED")}";
            statusMenuItem.ForeColor = adapter.IsEnabled ? MenuSuccess : MenuWarning;
        }

        enableMenuItem.Text = adapter is null
            ? "✓  Enable selected adapter"
            : $"✓  Enable {adapter.Name}";
        disableMenuItem.Text = adapter is null
            ? "⊘  Disable selected adapter"
            : $"⊘  Disable {adapter.Name}";

        enableMenuItem.ForeColor = MenuSuccess;
        disableMenuItem.ForeColor = Color.FromArgb(255, 139, 139);
        enableMenuItem.Enabled = adapter is not null && !adapter.IsEnabled && !_isBusy;
        disableMenuItem.Enabled = adapter is not null && adapter.IsEnabled && !_isBusy;
        enableMenuItem.ShortcutKeyDisplayString =
            adapter is not null && !adapter.IsEnabled ? "Ctrl+Alt+T" : string.Empty;
        disableMenuItem.ShortcutKeyDisplayString =
            adapter is not null && adapter.IsEnabled ? "Ctrl+Alt+T" : string.Empty;

        adaptersMenuItem.Enabled = !_adapterQueryFailed && _adapters.Count > 0;
        UpdateStartupMenuText();

        string trayText = adapter is null
            ? "InternetToggle - Adapter unavailable"
            : $"InternetToggle - {adapter.Name}: {(adapter.IsEnabled ? "Enabled" : "Disabled")}";

        notifyIcon1.Text = LimitTrayText(trayText);
        SetTrayIcon(adapter?.IsEnabled);
    }

    private void SetTrayIcon(bool? isEnabled)
    {
        if (_lastTrayIconState == isEnabled)
        {
            return;
        }

        _lastTrayIconState = isEnabled;

        Icon? nextIcon = isEnabled switch
        {
            true => LoadIconFromResource("connected.png"),
            false => LoadIconFromResource("disconnected.png"),
            _ => null
        };

        if (nextIcon is null)
        {
            notifyIcon1.Icon = SystemIcons.Application;
            _ownedTrayIcon?.Dispose();
            _ownedTrayIcon = null;
            return;
        }

        Icon? previousIcon = _ownedTrayIcon;
        _ownedTrayIcon = nextIcon;
        notifyIcon1.Icon = nextIcon;
        previousIcon?.Dispose();
    }

    private static Icon? LoadIconFromResource(string fileName)
    {
        string resourceName = $"{typeof(Form1).Namespace}.{fileName}";

        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return null;
        }

        using var bitmap = new Bitmap(stream);
        IntPtr iconHandle = bitmap.GetHicon();

        try
        {
            Icon temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "InternetToggle 1.0.0 is a small Windows system-tray utility for quickly enabling or " +
            "disabling a selected network adapter.\n\n" +
            "Hotkey: Ctrl+Alt+T\n\n" +
            "Important: this changes the selected adapter's administrative state. Another " +
            "adapter, VPN, or connection may still provide internet access.",
            "About InternetToggle",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private NetworkAdapterInfo? GetSelectedAdapter()
    {
        if (_selectedAdapterName is null)
        {
            return null;
        }

        return _adapters.FirstOrDefault(adapter =>
            adapter.Name.Equals(_selectedAdapterName, StringComparison.OrdinalIgnoreCase));
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return;
            }

            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(_settingsPath));

            _selectedAdapterName = settings?.SelectedAdapterName;
        }
        catch
        {
            _selectedAdapterName = null;
        }
    }

    private void SaveSettings(bool showErrors = true)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_settingsPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(new AppSettings
            {
                SelectedAdapterName = _selectedAdapterName
            }, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            if (showErrors)
            {
                MessageBox.Show(
                    this,
                    "The selected adapter could not be saved.\n\n" + ex.Message,
                    "InternetToggle",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private static string ToPowerShellString(string value) =>
        "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string LimitTrayText(string value) =>
        value.Length <= 63 ? value : value[..60] + "...";

    private static string GetPowerShellPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "System32",
        "WindowsPowerShell",
        "v1.0",
        "powershell.exe");

    private static string GetSchtasksPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "schtasks.exe");

    private static async Task<ProcessResult> RunPowerShellAsync(string script, TimeSpan timeout)
    {
        string command =
            "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " + script;

        return await RunProcessAsync(
            GetPowerShellPath(),
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command],
            timeout);
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {Path.GetFileName(fileName)}.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // The process may have exited between the check and Kill.
            }

            throw new TimeoutException(
                $"{Path.GetFileName(fileName)} did not finish within {timeout.TotalSeconds:0} seconds.");
        }

        await Task.WhenAll(outputTask, errorTask);

        return new ProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }

    private static string GetProcessError(ProcessResult result)
    {
        string details = string.Join(
            Environment.NewLine,
            new[] { result.ErrorText.Trim(), result.Output.Trim() }
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        return string.IsNullOrWhiteSpace(details)
            ? $"The command failed with exit code {result.ExitCode}."
            : details;
    }

    private sealed record NetworkAdapterInfo(
        string Name,
        string Description,
        string AdminStatus,
        string Status)
    {
        public bool IsEnabled =>
            AdminStatus.Equals("Up", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class AppSettings
    {
        public string? SelectedAdapterName { get; set; }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string ErrorText);
}
