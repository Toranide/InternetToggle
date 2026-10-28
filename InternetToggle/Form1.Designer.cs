namespace InternetToggle
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem statusMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disableMenuItem;
        private System.Windows.Forms.ToolStripMenuItem adaptersMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startupMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            notifyIcon1 = new System.Windows.Forms.NotifyIcon(components);
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            statusMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            enableMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            disableMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            adaptersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                statusMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                disableMenuItem,
                enableMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                adaptersMenuItem,
                startupMenuItem,
                aboutMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                exitMenuItem
            });

            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.ShowImageMargin = false;
            contextMenuStrip1.ShowCheckMargin = true;
            contextMenuStrip1.Size = new System.Drawing.Size(330, 300);

            statusMenuItem.Enabled = false;
            statusMenuItem.Name = "statusMenuItem";
            statusMenuItem.Size = new System.Drawing.Size(310, 38);
            statusMenuItem.Text = "●  NETWORK STATUS  ·  LOADING";

            enableMenuItem.Name = "enableMenuItem";
            enableMenuItem.Size = new System.Drawing.Size(310, 36);
            enableMenuItem.Text = "✓  Enable selected adapter";

            disableMenuItem.Name = "disableMenuItem";
            disableMenuItem.Size = new System.Drawing.Size(310, 36);
            disableMenuItem.Text = "⊘  Disable selected adapter";

            adaptersMenuItem.Name = "adaptersMenuItem";
            adaptersMenuItem.Size = new System.Drawing.Size(310, 36);
            adaptersMenuItem.Text = "⌁  Select network adapter";

            startupMenuItem.Name = "startupMenuItem";
            startupMenuItem.Size = new System.Drawing.Size(310, 36);
            startupMenuItem.Text = "⚙  Run at Windows sign-in";

            aboutMenuItem.Name = "aboutMenuItem";
            aboutMenuItem.Size = new System.Drawing.Size(310, 36);
            aboutMenuItem.Text = "ⓘ  About InternetToggle";

            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new System.Drawing.Size(310, 36);
            exitMenuItem.Text = "⏻  Exit";

            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = System.Drawing.SystemIcons.Application;
            notifyIcon1.Text = "InternetToggle";
            notifyIcon1.Visible = true;

            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            ClientSize = new System.Drawing.Size(200, 100);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            ShowInTaskbar = false;
            Text = "InternetToggle";
            WindowState = System.Windows.Forms.FormWindowState.Minimized;
        }
    }
}
