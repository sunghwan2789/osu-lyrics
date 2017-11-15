namespace osu_Lyrics
{
    partial class Lyrics
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.trayMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayMenu;
            this.trayIcon.Icon = global::osu_Lyrics.Properties.Resources.Icon;
            this.trayIcon.Text = "osu!Lyrics";
            this.trayIcon.Visible = true;
            this.trayIcon.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseUp);
            // 
            // trayMenu
            // 
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuSetting,
            this.menuExit});
            this.trayMenu.Name = "trayMenu";
            // 
            // menuSetting
            // 
            this.menuSetting.Name = "menuSetting";
            this.menuSetting.Text = "설정";
            this.menuSetting.Click += new System.EventHandler(this.menuSetting_Click);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Text = "종료";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 3000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Lyrics
            // 
            this.Name = "Lyrics";
            this.ShowInTaskbar = false;
            this.Text = "osu!Lyrics";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Lyrics_FormClosing);
            this.Load += new System.EventHandler(this.Lyrics_Load);
            this.Shown += new System.EventHandler(this.Lyrics_Shown);
            this.trayMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem menuSetting;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.Timer timer1;
    }
}