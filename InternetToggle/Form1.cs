using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Principal; // برای چک کردن دسترسی ادمین

namespace InternetToggle
{
    public partial class Form1 : Form
    {
        private const string AdapterName = "Ethernet";

        // Hotkey Settings
        private const int HOTKEY_ID = 1;
        private const int MOD_CONTROL = 0x2;
        private const int MOD_ALT = 0x1;
        private const int VK_T = 0x54;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        public Form1()
        {
            // ۱. بررسی دسترسی ادمین (اجباری برای قطع اینترنت و ساخت تسک استارتاپ)
            if (!IsAdministrator())
            {
                RestartAsAdmin();
                return; // برنامه فعلی بسته می‌شود تا نسخه ادمین باز شود
            }

            // ۲. اضافه کردن خودکار به استارتاپ ویندوز (بدون سوال پرسیدن در دفعات بعد)
            AddToStartup();

            InitializeComponent();

            notifyIcon1.DoubleClick += ToggleInternet;
            toggleMenuItem.Click += ToggleInternet;
            exitMenuItem.Click += Exit;

            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_T);

            UpdateUI();
        }

        // متد جدید: افزودن برنامه به Task Scheduler ویندوز برای اجرا با دسترسی ادمین هنگام لاگین
        private void AddToStartup()
        {
            try
            {
                string taskName = "InternetToggleAutoRun";
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // دستور ساخت تسک: اجرا هنگام لاگین (/sc onlogon) با بالاترین دسترسی (/rl highest)
                string args = $"/create /tn \"{taskName}\" /tr \"'{exePath}'\" /sc onlogon /rl highest /f";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
            }
            catch { /* خطا را نادیده می‌گیریم */ }
        }

        // متد جدید: بررسی ادمین بودن
        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // متد جدید: ریستارت برنامه با دسترسی ادمین
        private void RestartAsAdmin()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Process.GetCurrentProcess().MainModule.FileName;
            proc.Verb = "runas"; // درخواست ادمین

            try
            {
                Process.Start(proc);
                Application.Exit(); // بستن نسخه فعلی
            }
            catch
            {
                // اگر کاربر No را زد، برنامه بسته می‌شود
                Application.Exit();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleInternet(null, EventArgs.Empty);
            }
            base.WndProc(ref m);
        }

        private void ToggleInternet(object sender, EventArgs e)
        {
            bool enabled = IsEthernetEnabled();
            string cmd = enabled
                ? $"interface set interface \"{AdapterName}\" disable"
                : $"interface set interface \"{AdapterName}\" enable";

            RunNetsh(cmd);
            WaitForStatusChange(!enabled);
        }

        private void WaitForStatusChange(bool shouldBeEnabled)
        {
            int attempts = 0;
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) =>
            {
                attempts++;
                bool isEnabled = IsEthernetEnabled();
                UpdateUI();
                if (isEnabled == shouldBeEnabled || attempts >= 10)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private bool IsEthernetEnabled()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(n => n.Name.Equals(AdapterName, StringComparison.OrdinalIgnoreCase) &&
                          n.OperationalStatus == OperationalStatus.Up);
        }

        private void UpdateUI()
        {
            bool enabled = IsEthernetEnabled();

            // نام عکس‌ها در ریسورس
            string iconName = enabled ? "connected.png" : "disconnected.png";

            if (notifyIcon1.Icon != null)
            {
                DestroyIcon(notifyIcon1.Icon.Handle);
                notifyIcon1.Icon.Dispose();
            }

            notifyIcon1.Icon = LoadIconFromResource(iconName);

            notifyIcon1.Text = enabled ? "Ethernet: ON (Ctrl+Alt+T)" : "Ethernet: OFF (Ctrl+Alt+T)";
            toggleMenuItem.Text = enabled ? "Disable Internet ❌" : "Enable Internet ✅";
        }

        private Icon LoadIconFromResource(string fileName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // ساخت نام کامل ریسورس: Namespace.FileName
                string resourceName = $"{typeof(Form1).Namespace}.{fileName}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (Bitmap bmp = new Bitmap(stream))
                        {
                            return Icon.FromHandle(bmp.GetHicon());
                        }
                    }
                }
            }
            catch { }

            // اگر آیکون پیدا نشد یا ارور داد، آیکون پیش‌فرض سیستم را برمی‌گرداند
            return SystemIcons.Application;
        }

        private void RunNetsh(string args)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private void Exit(object sender, EventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
        }
    }
}

