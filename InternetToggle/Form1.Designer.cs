namespace InternetToggle
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toggleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toggleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.toggleMenuItem, this.exitMenuItem });
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 70);

            this.toggleMenuItem.Name = "toggleMenuItem";
            this.toggleMenuItem.Size = new System.Drawing.Size(180, 22);
            this.toggleMenuItem.Text = "Toggle Internet";

            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitMenuItem.Text = "Exit";

            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = System.Drawing.SystemIcons.Application;
            this.notifyIcon1.Text = "Internet Toggle";
            this.notifyIcon1.Visible = true;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(200, 100);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.ShowInTaskbar = false;
            this.Text = "Internet Toggle";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
        }
    }
}