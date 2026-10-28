using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InternetToggle;

internal sealed record TrayAdapterItem(
    string Name,
    string Description,
    bool IsEnabled,
    bool IsSelected);

internal sealed class TrayPopupForm : Form
{
    private static readonly Color BackgroundColor = Color.FromArgb(9, 20, 34);
    private static readonly Color CardColor = Color.FromArgb(14, 29, 47);
    private static readonly Color HoverColor = Color.FromArgb(20, 44, 69);
    private static readonly Color BorderColor = Color.FromArgb(40, 66, 93);
    private static readonly Color TextColor = Color.FromArgb(235, 244, 255);
    private static readonly Color MutedColor = Color.FromArgb(137, 163, 192);
    private static readonly Color BlueColor = Color.FromArgb(76, 189, 255);
    private static readonly Color GreenColor = Color.FromArgb(62, 220, 157);
    private static readonly Color RedColor = Color.FromArgb(255, 126, 139);
    private static readonly Font BrandFont = new("Segoe UI", 13.5F, FontStyle.Bold);
    private static readonly Font VersionFont = new("Segoe UI", 8.5F, FontStyle.Bold);
    private static readonly Font FooterFont = new("Segoe UI", 8F, FontStyle.Regular);

    private readonly IReadOnlyList<TrayAdapterItem> _adapters;
    private readonly string? _selectedAdapterName;
    private readonly bool _adapterQueryFailed;
    private readonly bool _isBusy;
    private readonly Func<bool, Task> _setAdapterEnabledAsync;
    private readonly Func<string, Task> _selectAdapterAsync;
    private readonly Func<Task> _toggleStartupAsync;
    private readonly Action _showAbout;
    private readonly Action _exitApplication;

    private readonly Label _brandInternet;
    private readonly Label _brandToggle;
    private readonly Label _versionLabel;
    private readonly AdapterStatusCard _statusCard;
    private readonly TrayActionButton _selectAdapterButton;
    private readonly TrayActionButton _disableButton;
    private readonly TrayActionButton _enableButton;
    private readonly TrayActionButton _startupButton;
    private readonly TrayActionButton _aboutButton;
    private readonly TrayActionButton _exitButton;
    private readonly TrayActionButton _backButton;
    private readonly Label _pageTitle;
    private readonly Label _footerLabel;
    private readonly TraySeparator _separatorOne;
    private readonly TraySeparator _separatorTwo;
    private readonly Panel _adapterListPanel;

    public TrayPopupForm(
        IReadOnlyList<TrayAdapterItem> adapters,
        string? selectedAdapterName,
        bool adapterQueryFailed,
        bool isBusy,
        bool startupEnabled,
        Func<bool, Task> setAdapterEnabledAsync,
        Func<string, Task> selectAdapterAsync,
        Func<Task> toggleStartupAsync,
        Action showAbout,
        Action exitApplication)
    {
        _adapters = adapters;
        _selectedAdapterName = selectedAdapterName;
        _adapterQueryFailed = adapterQueryFailed;
        _isBusy = isBusy;
        _setAdapterEnabledAsync = setAdapterEnabledAsync;
        _selectAdapterAsync = selectAdapterAsync;
        _toggleStartupAsync = toggleStartupAsync;
        _showAbout = showAbout;
        _exitApplication = exitApplication;

        AutoScaleMode = AutoScaleMode.None;
        BackColor = BackgroundColor;
        ClientSize = new Size(350, 458);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
        Text = "InternetToggle";

        _brandInternet = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = TextColor,
            Font = BrandFont,
            Location = new Point(17, 12),
            Text = "Internet"
        };
        _brandToggle = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = BlueColor,
            Font = BrandFont,
            Location = new Point(91, 12),
            Text = "Toggle"
        };
        _versionLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.FromArgb(18, 42, 66),
            ForeColor = BlueColor,
            Font = VersionFont,
            Text = "v1.0.0",
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(284, 16),
            Size = new Size(49, 23)
        };

        _statusCard = new AdapterStatusCard
        {
            Location = new Point(14, 49),
            Size = new Size(322, 60)
        };

        _selectAdapterButton = new TrayActionButton
        {
            Location = new Point(14, 116),
            Size = new Size(322, 46),
            Text = "Select network adapter",
            Subtitle = "Choose Ethernet, Wi-Fi or another adapter",
            Kind = TrayActionKind.Network,
            Accent = BlueColor
        };
        _selectAdapterButton.Click += (_, _) => ShowAdaptersView();

        _separatorOne = new TraySeparator
        {
            Location = new Point(25, 171),
            Size = new Size(300, 1)
        };

        _disableButton = new TrayActionButton
        {
            Location = new Point(14, 179),
            Size = new Size(322, 43),
            Text = "Disable adapter",
            Subtitle = "Disconnect this network interface",
            Kind = TrayActionKind.Disable,
            Accent = RedColor
        };
        _disableButton.Click += async (_, _) => await RunActionAsync(() => _setAdapterEnabledAsync(false));

        _enableButton = new TrayActionButton
        {
            Location = new Point(14, 224),
            Size = new Size(322, 43),
            Text = "Enable adapter",
            Subtitle = "Restore this network interface",
            Kind = TrayActionKind.Enable,
            Accent = GreenColor
        };
        _enableButton.Click += async (_, _) => await RunActionAsync(() => _setAdapterEnabledAsync(true));

        _separatorTwo = new TraySeparator
        {
            Location = new Point(25, 276),
            Size = new Size(300, 1)
        };

        _startupButton = new TrayActionButton
        {
            Location = new Point(14, 284),
            Size = new Size(322, 42),
            Text = "Run at Windows sign-in",
            Subtitle = "Start automatically when you log in",
            Kind = TrayActionKind.Startup,
            Accent = BlueColor,
            IsToggled = startupEnabled,
            ShowSwitch = true
        };
        _startupButton.Click += async (_, _) => await RunActionAsync(_toggleStartupAsync);

        _aboutButton = new TrayActionButton
        {
            Location = new Point(14, 331),
            Size = new Size(322, 35),
            Text = "About InternetToggle",
            Kind = TrayActionKind.About,
            Accent = MutedColor
        };
        _aboutButton.Click += (_, _) =>
        {
            Close();
            _showAbout();
        };

        _exitButton = new TrayActionButton
        {
            Location = new Point(14, 367),
            Size = new Size(322, 35),
            Text = "Exit",
            Kind = TrayActionKind.Exit,
            Accent = MutedColor
        };
        _exitButton.Click += (_, _) =>
        {
            Close();
            _exitApplication();
        };

        _footerLabel = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = MutedColor,
            Font = FooterFont,
            Location = new Point(18, 422),
            Size = new Size(314, 20),
            Text = "ONE ADAPTER AT A TIME  ·  BUILT FOR FOCUSED WORK",
            TextAlign = ContentAlignment.MiddleCenter
        };

        _backButton = new TrayActionButton
        {
            Location = new Point(13, 10),
            Size = new Size(32, 31),
            Text = string.Empty,
            Kind = TrayActionKind.Back,
            Accent = BlueColor,
            Visible = false
        };
        _backButton.Click += (_, _) => ShowMainView();

        _pageTitle = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = TextColor,
            Font = BrandFont,
            Location = new Point(53, 12),
            Size = new Size(260, 28),
            Text = "Choose adapter",
            Visible = false
        };

        _adapterListPanel = new Panel
        {
            AutoScroll = true,
            BackColor = BackgroundColor,
            Location = new Point(14, 52),
            Size = new Size(322, 388),
            Visible = false
        };

        Controls.AddRange(
        [
            _brandInternet,
            _brandToggle,
            _versionLabel,
            _statusCard,
            _selectAdapterButton,
            _separatorOne,
            _disableButton,
            _enableButton,
            _separatorTwo,
            _startupButton,
            _aboutButton,
            _exitButton,
            _footerLabel,
            _backButton,
            _pageTitle,
            _adapterListPanel
        ]);

        RefreshMainView();
        ApplyRoundedRegion();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return parameters;
        }
    }

    public void ShowAt(Point cursorPosition)
    {
        Screen screen = Screen.FromPoint(cursorPosition);
        Rectangle area = screen.WorkingArea;

        int x = cursorPosition.X - Width + 18;
        int y = cursorPosition.Y - Height - 8;

        if (x + Width > area.Right)
        {
            x = area.Right - Width - 8;
        }

        if (x < area.Left)
        {
            x = area.Left + 8;
        }

        if (y < area.Top)
        {
            y = cursorPosition.Y + 10;
        }

        if (y + Height > area.Bottom)
        {
            y = area.Bottom - Height - 8;
        }

        Location = new Point(x, y);
        Show();
        Activate();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (!IsDisposed)
        {
            BeginInvoke(new Action(() =>
            {
                if (!IsDisposed && !ContainsFocus)
                {
                    Close();
                }
            }));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using GraphicsPath path = RoundedRectangle(
            new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), 15);
        using var pen = new Pen(BorderColor, 1);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    private void ApplyRoundedRegion()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using GraphicsPath path = RoundedRectangle(
            new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 15);
        Region = new Region(path);
    }

    private void RefreshMainView()
    {
        TrayAdapterItem? selected = _adapters.FirstOrDefault(adapter =>
            string.Equals(adapter.Name, _selectedAdapterName, StringComparison.OrdinalIgnoreCase));

        _statusCard.AdapterName = selected?.Name ?? (_adapterQueryFailed ? "Adapters unavailable" : "No adapter selected");
        _statusCard.StateText = selected is null
            ? "CHECK NETWORK ADAPTERS"
            : selected.IsEnabled ? "ADAPTER ENABLED" : "ADAPTER DISABLED";
        _statusCard.StateColor = selected is null
            ? RedColor
            : selected.IsEnabled ? GreenColor : RedColor;
        _statusCard.Invalidate();

        _selectAdapterButton.Subtitle = selected is null
            ? "Choose Ethernet, Wi-Fi or another adapter"
            : $"Currently selected: {selected.Name}";

        _disableButton.Text = selected is null ? "Disable adapter" : $"Disable {selected.Name}";
        _enableButton.Text = selected is null ? "Enable adapter" : $"Enable {selected.Name}";
        _disableButton.Shortcut = selected is not null && selected.IsEnabled ? "CTRL + ALT + T" : string.Empty;
        _enableButton.Shortcut = selected is not null && !selected.IsEnabled ? "CTRL + ALT + T" : string.Empty;
        _disableButton.Enabled = selected is not null && selected.IsEnabled && !_isBusy;
        _enableButton.Enabled = selected is not null && !selected.IsEnabled && !_isBusy;
        _selectAdapterButton.Enabled = !_adapterQueryFailed && _adapters.Count > 0;
        _disableButton.Invalidate();
        _enableButton.Invalidate();
    }

    private void ShowAdaptersView()
    {
        _brandInternet.Visible = false;
        _brandToggle.Visible = false;
        _versionLabel.Visible = false;
        _statusCard.Visible = false;
        _selectAdapterButton.Visible = false;
        _separatorOne.Visible = false;
        _disableButton.Visible = false;
        _enableButton.Visible = false;
        _separatorTwo.Visible = false;
        _startupButton.Visible = false;
        _aboutButton.Visible = false;
        _exitButton.Visible = false;
        _footerLabel.Visible = false;

        _backButton.Visible = true;
        _pageTitle.Visible = true;
        _adapterListPanel.Visible = true;
        BuildAdapterList();
    }

    private void ShowMainView()
    {
        _brandInternet.Visible = true;
        _brandToggle.Visible = true;
        _versionLabel.Visible = true;
        _statusCard.Visible = true;
        _selectAdapterButton.Visible = true;
        _separatorOne.Visible = true;
        _disableButton.Visible = true;
        _enableButton.Visible = true;
        _separatorTwo.Visible = true;
        _startupButton.Visible = true;
        _aboutButton.Visible = true;
        _exitButton.Visible = true;
        _footerLabel.Visible = true;

        _backButton.Visible = false;
        _pageTitle.Visible = false;
        _adapterListPanel.Visible = false;
        RefreshMainView();
    }

    private void BuildAdapterList()
    {
        _adapterListPanel.SuspendLayout();
        _adapterListPanel.Controls.Clear();

        if (_adapters.Count == 0)
        {
            var empty = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(8, 12),
                Size = new Size(288, 48),
                Text = _adapterQueryFailed
                    ? "Windows could not return the adapter list."
                    : "No network adapters were found.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _adapterListPanel.Controls.Add(empty);
        }
        else
        {
            int y = 4;
            foreach (TrayAdapterItem adapter in _adapters)
            {
                var row = new TrayActionButton
                {
                    Location = new Point(2, y),
                    Size = new Size(294, 58),
                    Text = adapter.Name,
                    Subtitle = adapter.IsSelected
                        ? $"{(adapter.IsEnabled ? "Enabled" : "Disabled")}  ·  Selected"
                        : adapter.IsEnabled ? "Enabled" : "Disabled",
                    Kind = TrayActionKind.Network,
                    Accent = adapter.IsSelected ? BlueColor : adapter.IsEnabled ? GreenColor : MutedColor,
                    IsSelectedRow = adapter.IsSelected
                };

                string adapterName = adapter.Name;
                row.Click += async (_, _) =>
                {
                    Close();
                    await _selectAdapterAsync(adapterName);
                };

                _adapterListPanel.Controls.Add(row);
                y += 64;
            }
        }

        _adapterListPanel.ResumeLayout();
    }

    private async Task RunActionAsync(Func<Task> action)
    {
        Close();
        await action();
    }

    internal static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal enum TrayActionKind
{
    Network,
    Enable,
    Disable,
    Startup,
    About,
    Exit,
    Back
}

internal sealed class TrayActionButton : Control
{
    private static readonly Color TextColor = Color.FromArgb(235, 244, 255);
    private static readonly Color MutedColor = Color.FromArgb(137, 163, 192);
    private static readonly Color HoverColor = Color.FromArgb(20, 44, 69);
    private static readonly Color SelectedColor = Color.FromArgb(17, 43, 68);
    private static readonly Font TitleFont = new("Segoe UI", 9.2F, FontStyle.Regular);
    private static readonly Font SubtitleFont = new("Segoe UI", 7.8F, FontStyle.Regular);
    private static readonly Font ShortcutFont = new("Segoe UI", 7.1F, FontStyle.Bold);

    private bool _hovered;

    public new string Text { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Shortcut { get; set; } = string.Empty;
    public TrayActionKind Kind { get; set; }
    public Color Accent { get; set; } = Color.FromArgb(76, 189, 255);
    public bool ShowSwitch { get; set; }
    public bool IsToggled { get; set; }
    public bool IsSelectedRow { get; set; }

    public TrayActionButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle full = new(0, 0, Width - 1, Height - 1);
        Color back = IsSelectedRow ? SelectedColor : _hovered && Enabled
            ? HoverColor
            : Color.FromArgb(9, 20, 34);

        using (GraphicsPath background = TrayPopupForm.RoundedRectangle(full, 9))
        using (var brush = new SolidBrush(back))
        {
            graphics.FillPath(brush, background);
            if (IsSelectedRow || (_hovered && Enabled))
            {
                using var border = new Pen(IsSelectedRow ? Accent : Color.FromArgb(37, 72, 105));
                graphics.DrawPath(border, background);
            }
        }

        Color iconColor = Enabled ? Accent : Color.FromArgb(77, 94, 114);
        DrawIcon(graphics, new Rectangle(12, (Height - 22) / 2, 22, 22), iconColor);

        int left = Kind == TrayActionKind.Back ? 10 : 44;
        if (Kind == TrayActionKind.Back)
        {
            return;
        }

        int rightReserve = !string.IsNullOrWhiteSpace(Shortcut)
            ? 120
            : ShowSwitch
                ? 58
                : IsSelectedRow
                    ? 42
                    : 22;

        if (!string.IsNullOrEmpty(Subtitle))
        {
            DrawText(
                graphics,
                Text,
                new Rectangle(left, 5, Math.Max(20, Width - left - rightReserve), 18),
                TitleFont,
                Enabled ? TextColor : MutedColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            DrawText(
                graphics,
                Subtitle,
                new Rectangle(left, 23, Math.Max(20, Width - left - rightReserve), 15),
                SubtitleFont,
                MutedColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else
        {
            DrawText(
                graphics,
                Text,
                new Rectangle(left, 0, Math.Max(20, Width - left - rightReserve), Height),
                TitleFont,
                Enabled ? TextColor : MutedColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        if (!string.IsNullOrWhiteSpace(Shortcut) && Enabled)
        {
            Size textSize = TextRenderer.MeasureText(Shortcut, ShortcutFont);
            int badgeWidth = Math.Min(105, textSize.Width + 12);
            var badge = new Rectangle(Width - badgeWidth - 8, (Height - 22) / 2, badgeWidth, 22);

            using GraphicsPath badgePath = TrayPopupForm.RoundedRectangle(badge, 5);
            using var badgeBrush = new SolidBrush(Color.FromArgb(22, 46, 70));
            using var badgePen = new Pen(Color.FromArgb(39, 76, 110));
            graphics.FillPath(badgeBrush, badgePath);
            graphics.DrawPath(badgePen, badgePath);

            DrawText(
                graphics,
                Shortcut,
                badge,
                ShortcutFont,
                Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        if (ShowSwitch)
        {
            DrawSwitch(graphics, new Rectangle(Width - 47, (Height - 20) / 2, 34, 20));
        }
        else if (IsSelectedRow)
        {
            using var selectedBrush = new SolidBrush(Accent);
            graphics.FillEllipse(selectedBrush, Width - 26, (Height - 7) / 2, 7, 7);
        }

        if (Focused)
        {
            using var focusPen = new Pen(Color.FromArgb(80, Accent), 1)
            {
                DashStyle = DashStyle.Dot
            };
            graphics.DrawRectangle(focusPen, 4, 4, Width - 9, Height - 9);
        }
    }

    private void DrawSwitch(Graphics graphics, Rectangle bounds)
    {
        Color trackColor = IsToggled ? Color.FromArgb(23, 126, 91) : Color.FromArgb(39, 57, 76);
        using GraphicsPath path = TrayPopupForm.RoundedRectangle(bounds, 10);
        using var track = new SolidBrush(trackColor);
        using var trackPen = new Pen(IsToggled ? Color.FromArgb(59, 211, 153) : Color.FromArgb(73, 95, 119));
        graphics.FillPath(track, path);
        graphics.DrawPath(trackPen, path);

        int x = IsToggled ? bounds.Right - 17 : bounds.X + 3;
        using var thumb = new SolidBrush(IsToggled ? Color.White : Color.FromArgb(171, 191, 213));
        graphics.FillEllipse(thumb, x, bounds.Y + 3, 14, 14);
    }

    private static void DrawText(
        Graphics graphics,
        string text,
        Rectangle bounds,
        Font font,
        Color color,
        TextFormatFlags flags)
    {
        TextRenderer.DrawText(graphics, text, font, bounds, color, flags);
    }

    private void DrawIcon(Graphics graphics, Rectangle bounds, Color color)
    {
        using var pen = new Pen(color, 1.8F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        float cx = bounds.Left + bounds.Width / 2F;
        float cy = bounds.Top + bounds.Height / 2F;

        switch (Kind)
        {
            case TrayActionKind.Network:
                graphics.DrawLine(pen, cx, cy - 5, cx, cy + 3);
                graphics.DrawLine(pen, cx, cy - 1, cx - 6, cy - 6);
                graphics.DrawLine(pen, cx, cy - 1, cx + 6, cy - 6);
                graphics.DrawLine(pen, cx, cy + 3, cx - 6, cy + 8);
                graphics.DrawLine(pen, cx, cy + 3, cx + 6, cy + 8);
                DrawNode(graphics, cx - 6, cy - 6, color);
                DrawNode(graphics, cx + 6, cy - 6, color);
                DrawNode(graphics, cx, cy - 1, color);
                DrawNode(graphics, cx - 6, cy + 8, color);
                DrawNode(graphics, cx + 6, cy + 8, color);
                break;

            case TrayActionKind.Enable:
                graphics.DrawArc(pen, cx - 8, cy - 8, 16, 16, 40, 280);
                graphics.DrawLine(pen, cx, cy - 10, cx, cy - 1);
                break;

            case TrayActionKind.Disable:
                graphics.DrawEllipse(pen, cx - 8, cy - 8, 16, 16);
                graphics.DrawLine(pen, cx - 5, cy + 5, cx + 5, cy - 5);
                break;

            case TrayActionKind.Startup:
                graphics.DrawArc(pen, cx - 8, cy - 8, 16, 16, 15, 300);
                graphics.DrawLine(pen, cx, cy - 10, cx, cy - 5);
                graphics.DrawLine(pen, cx + 8, cy, cx + 10, cy);
                break;

            case TrayActionKind.About:
                graphics.DrawEllipse(pen, cx - 8, cy - 8, 16, 16);
                using (var dotBrush = new SolidBrush(color))
                {
                    graphics.FillEllipse(dotBrush, cx - 1.2F, cy - 4.8F, 2.4F, 2.4F);
                }
                graphics.DrawLine(pen, cx, cy - 1, cx, cy + 5);
                break;

            case TrayActionKind.Exit:
                graphics.DrawArc(pen, cx - 8, cy - 8, 16, 16, 45, 270);
                graphics.DrawLine(pen, cx, cy - 10, cx, cy - 1);
                break;

            case TrayActionKind.Back:
                graphics.DrawLine(pen, cx + 4, cy - 6, cx - 3, cy);
                graphics.DrawLine(pen, cx - 3, cy, cx + 4, cy + 6);
                break;
        }
    }

    private static void DrawNode(Graphics graphics, float x, float y, Color color)
    {
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, x - 2.3F, y - 2.3F, 4.6F, 4.6F);
    }
}

internal sealed class AdapterStatusCard : Control
{
    private static readonly Color CardColor = Color.FromArgb(14, 29, 47);
    private static readonly Color BorderColor = Color.FromArgb(40, 66, 93);
    private static readonly Color TextColor = Color.FromArgb(235, 244, 255);
    private static readonly Color MutedColor = Color.FromArgb(137, 163, 192);
    private static readonly Font CaptionFont = new("Segoe UI", 7.4F, FontStyle.Bold);
    private static readonly Font NameFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font StateFont = new("Segoe UI", 7.2F, FontStyle.Bold);

    public string AdapterName { get; set; } = "Loading network adapters";
    public string StateText { get; set; } = "CHECKING STATUS";
    public Color StateColor { get; set; } = Color.FromArgb(76, 189, 255);

    public AdapterStatusCard()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = CardColor;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using GraphicsPath path = TrayPopupForm.RoundedRectangle(
            new Rectangle(0, 0, Width - 1, Height - 1), 10);
        using var background = new SolidBrush(CardColor);
        using var border = new Pen(BorderColor);

        g.FillPath(background, path);
        g.DrawPath(border, path);

        using (var dotBrush = new SolidBrush(StateColor))
        {
            g.FillEllipse(dotBrush, 15, 18, 8, 8);
        }

        TextRenderer.DrawText(
            g,
            "SELECTED ADAPTER",
            CaptionFont,
            new Rectangle(30, 8, 180, 15),
            MutedColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            g,
            AdapterName,
            NameFont,
            new Rectangle(30, 25, Math.Max(40, Width - 145), 25),
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var pill = new Rectangle(Width - 108, 20, 95, 22);
        using GraphicsPath pillPath = TrayPopupForm.RoundedRectangle(pill, 6);
        using var pillBrush = new SolidBrush(Color.FromArgb(24, StateColor.R, StateColor.G, StateColor.B));
        using var pillPen = new Pen(Color.FromArgb(85, StateColor.R, StateColor.G, StateColor.B));
        g.FillPath(pillBrush, pillPath);
        g.DrawPath(pillPen, pillPath);

        TextRenderer.DrawText(
            g,
            StateText,
            StateFont,
            pill,
            StateColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class TraySeparator : Control
{
    public TraySeparator()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.FromArgb(9, 20, 34);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(40, 61, 82));
        e.Graphics.DrawLine(pen, 0, 0, Width, 0);
    }
}
