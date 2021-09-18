using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Net;
using System.Diagnostics;

namespace ShortNotes
{
    public partial class Form1 : Form
    {
        public bool close = false;
        public bool onlyTray = false;
        public bool clean = false;
        public bool silent = false;

        private string searchTxt = "";

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!silent) ((nTabPage)Tabs.SelectedTab).startBackgroundWorker(true);
            ResetOrder();
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {

            }
            else if (close)
            {

            }
            else
                e.Cancel = true;
            Application.ExitThread();
            base.OnFormClosing(e);
        }
        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated) CreateHandle();
            base.SetVisibleCore(!onlyTray);
            int sel = 0;
            if (!clean && Tabs.TabCount >= 1)
            {
                //loading last files here
                if (System.IO.File.Exists(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), "index")))
                {
                    int i = 0;
                    using (var filestream = System.IO.File.OpenRead(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), "index")))
                    using (var streamReader = new StreamReader(filestream, Encoding.ASCII, true))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] args = line.Split("|||");
                            bool a = false;
                            bool b = false;
                            if (args[2] == "True") a = true;
                            if (args[3] == "True") b = true;
                            if (args[4] == "True") sel = i;
                            newTab(true, args[0], args[1], a, b);//!!!
                            i++;
                        }
                    }
                }
            }
            else if (!silent)
            {
                foreach (string file in Directory.GetFiles(Path.Combine(Application.StartupPath, "tmp")))
                    System.IO.File.Delete(file);
            }

            if (Tabs.TabCount == 0)
            {
                newTab();
            }


            this.KeyPreview = true;
            lastTabIndex = sel;
            Tabs.SelectTab(Tabs.TabPages[sel]);

            if (!((nTabPage)Tabs.SelectedTab).enc || ((nTabPage)Tabs.SelectedTab).decRn)
                ((nTabPage)Tabs.SelectedTab).txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Encrypt Tab";
            else
                ((nTabPage)Tabs.SelectedTab).txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Decrypt Tab";

            ((nTabPage)Tabs.TabPages[sel]).txtBox.Focus();
        }
        protected override void OnActivated(EventArgs e)
        {
            if (Tabs.TabCount > 0) { ((nTabPage)Tabs.SelectedTab).txtBox.Focus(); }
            base.OnActivated(e);
        }

        KeyboardHook hook = new KeyboardHook(); //https://stackoverflow.com/questions/2450373/set-global-hotkeys-using-c-sharp

        NotifyIcon TrayIcon = new NotifyIcon();
        ContextMenuStrip TrayIconContextMenu = new ContextMenuStrip();
        ToolStripMenuItem MenuItemExit = new ToolStripMenuItem();
        ToolStripMenuItem MenuItemShow = new ToolStripMenuItem();

        ToolStripMenuItem AlwaysOnTop = new ToolStripMenuItem();
        ToolStripMenuItem StartMenu = new ToolStripMenuItem();
        ToolStripMenuItem Startup = new ToolStripMenuItem();
        ToolStripMenuItem SearchUpdates = new ToolStripMenuItem();

        private string TxtMode = "";
        private int lastTabIndex;

        public Form1()
        {
            InitializeComponent();

            if (!Directory.Exists(Path.Combine(Application.StartupPath, "tmp"))) { Directory.CreateDirectory(Path.Combine(Application.StartupPath, "tmp")); }

            hook.RegisterHotKey(global::ModifierKeys.Control, Keys.OemPeriod);
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            #region Tray Stuff: https://www.codeproject.com/tips/627796/doing-a-notifyicon-program-the-right-way
            TrayIcon.Text = "ShortNotes";
            TrayIcon.Icon = Properties.Resources.ShortNotes;
            TrayIcon.MouseDown += TrayIcon_Click;

            TrayIconContextMenu.SuspendLayout();
            TrayIconContextMenu.ShowImageMargin = false;

            TrayIconContextMenu.Name = "ShortNotes";
            TrayIconContextMenu.Size = new Size(153, 70);
            TrayIconContextMenu.BackColor = Color.Black;
            TrayIconContextMenu.ForeColor = Color.White;

            MenuItemExit.Name = "Exit";
            MenuItemExit.Size = new Size(153, 22);
            MenuItemExit.Text = "Exit";
            MenuItemExit.ForeColor = Color.White;
            MenuItemExit.Click += new EventHandler(BtnExit_Click);

            MenuItemShow.Name = "Show";
            MenuItemShow.Size = new Size(153, 22);
            if (this.ShowInTaskbar)
            {
                MenuItemShow.Text = "Show";
            }
            else
            {
                MenuItemShow.Text = "Hide";
            }
            MenuItemShow.ForeColor = Color.White;
            MenuItemShow.Click += new EventHandler(TrayShow);

            TrayIconContextMenu.Items.AddRange(new ToolStripItem[]
            {
                MenuItemShow, MenuItemExit,
            });

            TrayIconContextMenu.ResumeLayout(false);

            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
            TrayIcon.Visible = true;
            #endregion

            #region Right Click Menu for Form
            var contextMenuTop = new ContextMenuStrip();
            contextMenuTop.SuspendLayout();
            contextMenuTop.ShowImageMargin = false;
            contextMenuTop.BackColor = Color.Black;
            contextMenuTop.ForeColor = Color.White;

            AlwaysOnTop.Text = "Always On Top: off";
            AlwaysOnTop.Click += AlwaysOnTop_Click;

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes", "ShortNotes" + ".lnk")))
                StartMenu.Text = "Delete Start Menu";
            else
                StartMenu.Text = "Create Start Menu";
            StartMenu.Click += StartMenu_Click;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk.GetValueNames().Contains("ShortNotes"))
                Startup.Text = "Delete Startup entry";
            else
                Startup.Text = "Create Startup entry";
            Startup.Click += Startup_Click;

            SearchUpdates.Text = "Search for updates";
            SearchUpdates.Click += SearchUpdates_Click;
            int offlineVersion = Int16.Parse(Application.ProductVersion.Replace(".", "").Replace("v", ""));
            int onlineVersion = Int16.Parse(new WebClient().DownloadString("https://raw.githubusercontent.com/jalupaja/ShortNotes/main/ShortNotes/VersionNumber.txt"));
            if (offlineVersion < onlineVersion)
            {
                SearchUpdates.Text = "Install update";
            }

            //!!!

            contextMenuTop.Items.AddRange(new ToolStripItem[]
            {
                AlwaysOnTop, StartMenu, Startup, SearchUpdates
            });
            contextMenuTop.ResumeLayout(false);
            this.ContextMenuStrip = contextMenuTop;
            #endregion

            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            Tabs.Padding = new Point(0, 0);

            this.KeyPreview = true;
        }

        private void SearchUpdates_Click(object sender, EventArgs e)
        {
            int offlineVersion = Int16.Parse(Application.ProductVersion.Replace(".", "").Replace("v", ""));
            int onlineVersion = Int16.Parse(new WebClient().DownloadString("https://raw.githubusercontent.com/jalupaja/ShortNotes/main/ShortNotes/VersionNumber.txt"));
            if (offlineVersion < onlineVersion)
            {
                try
                {
                    Process.Start("Updater.exe", "\"https://github.com/jalupaja/ShortNotes/releases/latest/download/ShortNotes.zip\" question");
                }
                catch (Exception) { }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {//https://www.codeproject.com/Tips/543631/Save-and-restore-your-form-size-and-location
            string initLocation = Properties.Settings.Default.oldLocation;
            Point il = new Point(0, 0);
            Size sz = Size;
            if (!string.IsNullOrWhiteSpace(initLocation))
            {
                string[] parts = initLocation.Split(',');
                if (parts.Length >= 2)
                {
                    il = new Point(int.Parse(parts[0]), int.Parse(parts[1]));
                }
                if (parts.Length >= 4)
                {
                    sz = new Size(int.Parse(parts[2]), int.Parse(parts[3]));
                }
            }
            Size = sz;
            Location = il;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Point location = Location;
            Size size = Size;
            if (WindowState != FormWindowState.Normal)
            {
                location = RestoreBounds.Location;
                size = RestoreBounds.Size;
            }
            string initLocation = string.Join(",", location.X, location.Y, size.Width, size.Height);
            Properties.Settings.Default.oldLocation = initLocation;
            Properties.Settings.Default.Save();
        }

        private void Startup_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk.GetValueNames().Contains("ShortNotes"))
            {
                try
                {
                    rk.DeleteValue("ShortNotes", false);
                    Startup.Text = "Create Startup entry";
                }
                catch (Exception)
                {
                    Startup.Text = "Delete Startup entry";
                    MessageBox.Show("Insufficient Permissions!");
                }
            }
            else
            {
                try
                {
                    rk.SetValue("ShortNotes", Application.ExecutablePath + " -s");
                    Startup.Text = "Delete Startup entry";
                }
                catch (Exception)
                {
                    Startup.Text = "Create Startup entry";
                    MessageBox.Show("Insufficient Permissions!");
                }
            }
        }

        private void StartMenu_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes", "ShortNotes" + ".lnk")))
            {
                try
                {
                    System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes", "ShortNotes" + ".lnk"));
                    Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes"), false); // Dont delete folder if another program uses it
                }
                catch (Exception)
                { }
            }
            else
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes", "ShortNotes" + ".lnk"));
                try
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "ShortNotes"));
                    shortcut.Description = "A simple Notes program";
                    shortcut.IconLocation = System.Windows.Forms.Application.ExecutablePath;
                    shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                    shortcut.Save();
                    StartMenu.Text = "Delete Start Menu";
                }
                catch (Exception)
                {
                    StartMenu.Text = "Create Start Menu";
                    MessageBox.Show("Insufficient Permissions!");
                }
            }
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (onlyTray || Focused || ((nTabPage)Tabs.SelectedTab).txtBox.Focused)
                TrayShow(null, null);
            else
                this.Activate();
        }

        private void AlwaysOnTop_Click(object sender, EventArgs e)
        {
            if (AlwaysOnTop.Text.Contains("off"))
            {
                TopMost = true;
                AlwaysOnTop.Text = "Always On Top: on";
            }
            else
            {
                TopMost = false;
                AlwaysOnTop.Text = "Always On Top: off";
            }
        }

        public void TrayShow(object sender, EventArgs e)
        {
            if (onlyTray)
            {
                onlyTray = false;
                base.SetVisibleCore(true);
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
                MenuItemShow.Text = "Hide";
                ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
            }
            else
            {
                onlyTray = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                MenuItemShow.Text = "Show";
            }
        }

        private void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TrayShow(null, null);
            }
        }

        #region move and resize stuff by: https://stackoverflow.com/questions/2575216/how-to-move-and-resize-a-form-without-a-border
        private const int cGrip = 20;      // Grip size
        private const int cCaption = 12;   // Caption bar height;

        Rectangle TopLeft { get { return new Rectangle(0, 0, cGrip, cGrip); } }
        Rectangle BottomLeft { get { return new Rectangle(0, this.ClientSize.Height - cGrip, cGrip, cGrip); } }
        Rectangle BottomRight { get { return new Rectangle(this.ClientSize.Width - cGrip, this.ClientSize.Height - cGrip, cGrip, cGrip); } }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == 0x84) // WM_NCHITTEST
            {
                var cursor = this.PointToClient(Cursor.Position);
                if (TopLeft.Contains(cursor)) message.Result = (IntPtr)13;
                else if (BottomLeft.Contains(cursor)) message.Result = (IntPtr)16;
                else if (BottomRight.Contains(cursor)) message.Result = (IntPtr)17;

                if (cursor.Y < cCaption)
                {
                    message.Result = (IntPtr)2;  // HTCAPTION
                    return;
                }
            }
        }
        #endregion

        private void BtnExit_Click(object sender, EventArgs e)
        {
            close = true;
            Application.Exit();
        }

        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            TrayShow(null, null);
        }


        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!((nTabPage)Tabs.SelectedTab).saved && !silent && Tabs.TabCount != lastTabIndex) { ((nTabPage)Tabs.TabPages[lastTabIndex]).startBackgroundWorker(true); }

            #region see if file changed/ still exists
            if (((nTabPage)Tabs.SelectedTab).location != "")
            {
                if (System.IO.File.Exists(((nTabPage)Tabs.SelectedTab).location))
                {
                    if (!((nTabPage)Tabs.SelectedTab).saved)
                    {
                        ((nTabPage)Tabs.SelectedTab).backgroundWorker2_DoWork(null, null);
                    }
                }
                else
                {
                    var msg = MessageBox.Show("This file doesn't exist anymore.\nDo you want to keep it in the editor?", "file doesn't exist anymore", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (msg == DialogResult.No)
                    {
                        string filename = "";
                        if (((nTabPage)Tabs.SelectedTab).location == "")
                            filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).name));
                        else
                            filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).location));
                        try { System.IO.File.Delete(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename)); } catch (Exception) { }

                        int index = Tabs.SelectedIndex;
                        var tab = Tabs.SelectedTab;
                        if (Tabs.TabCount == 1)
                        {
                            newTab();
                        }
                        Tabs.Controls.Remove(tab);

                        if (index == 0)
                            Tabs.SelectedIndex = 0;
                        else
                            Tabs.SelectedIndex = index - 1;
                        ResetOrder();
                    }
                }
            }
            #endregion

            if (!((nTabPage)Tabs.SelectedTab).enc || ((nTabPage)Tabs.SelectedTab).decRn)
                ((nTabPage)Tabs.SelectedTab).txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Encrypt Tab";
            else
                ((nTabPage)Tabs.SelectedTab).txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Decrypt Tab";

            ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
            lastTabIndex = Tabs.Controls.IndexOf(Tabs.SelectedTab);

            #region tried "smart tabcontrol": not working
            /*
            if ((DateTime.Now - lastTimeNewPage).TotalMilliseconds < 50 || (Tabs.Controls.IndexOf(Tabs.SelectedTab) != lastTabIndex -1 && Tabs.Controls.IndexOf(Tabs.SelectedTab) != lastTabIndex + 1 && (lastTabIndex == 0 && Tabs.Controls.IndexOf(Tabs.SelectedTab) == Tabs.TabCount-1) && (lastTabIndex == Tabs.TabCount - 1 && Tabs.Controls.IndexOf(Tabs.SelectedTab) == 0)))
            {
                //safe file to local folder
                ((nTabPage)Tabs.TabPages[lastTabIndex]).startBackgroundWorker();
            }
            else
            {
                if ((DateTime.Now - lastTabTime).TotalMilliseconds > 750)
                {
                    //safe file to local folder
                    ((nTabPage)Tabs.TabPages[lastTabIndex]).startBackgroundWorker();
                    if (secondLastTabIndex == Tabs.Controls.IndexOf(Tabs.SelectedTab))
                    {
                        ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
                    }
                    else if (Tabs.TabPages[secondLastTabIndex] != null)
                    {
                        Tabs.SelectTab(secondLastTabIndex);
                        ((nTabPage)Tabs.TabPages[secondLastTabIndex]).txtBox.Focus();
                    }
                    else
                        ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
                }
                else if ((DateTime.Now - lastTabTime).TotalMilliseconds > 10)
                {
                    //safe file to local folder
                    ((nTabPage)Tabs.SelectedTab).startBackgroundWorker();
                    if (Tabs.Controls.IndexOf(Tabs.SelectedTab) == secondLastTabIndex)
                    {
                        if (lastTabIndex == secondLastTabIndex - 1 || (secondLastTabIndex == Tabs.Controls.IndexOf(Tabs.SelectedTab) && lastTabIndex == 0))
                        {
                            if (secondLastTabIndex == Tabs.TabCount - 1)
                            {
                                Tabs.SelectTab(0);
                                ((nTabPage)Tabs.TabPages[0]).txtBox.Focus();
                            }
                            else
                            {
                                Tabs.SelectTab(secondLastTabIndex + 1);
                                ((nTabPage)Tabs.TabPages[secondLastTabIndex]).txtBox.Focus();
                            }
                        }
                        else
                        {
                            if (secondLastTabIndex == 0)
                            {
                                Tabs.SelectTab(Tabs.TabCount - 1);
                                ((nTabPage)Tabs.TabPages[Tabs.TabCount - 1]).txtBox.Focus();
                            }
                            else
                            {
                                Tabs.SelectTab(secondLastTabIndex - 1);
                                ((nTabPage)Tabs.TabPages[secondLastTabIndex - 1]).txtBox.Focus();
                            }
                        }
                    }
                }
                else
                    return;
            }
            lastTabTime = DateTime.Now;
            secondLastTabIndex = lastTabIndex;
            lastTabIndex = Tabs.Controls.IndexOf(Tabs.SelectedTab);
            */
            #endregion
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) //local Hotkeys
        {
            if (e.Control && (e.KeyCode == Keys.N || e.KeyCode == Keys.T))
            {
                newTab();
                ResetOrder();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                if (silent)
                {
                    int i = Tabs.SelectedIndex;
                    var t = Tabs.SelectedTab;
                    if (Tabs.TabCount == 1)
                    {
                        newTab();
                    }
                    Tabs.Controls.Remove(t);

                    if (i == 0)
                        Tabs.SelectedIndex = 0;
                    else
                        Tabs.SelectedIndex = i - 1;
                    e.SuppressKeyPress = true;
                    return;
                }
                if (!((nTabPage)Tabs.SelectedTab).saved && ((nTabPage)Tabs.SelectedTab).txtBox.Text != "\n\n\n\n\n\n\n\n\n\n")
                {
                    var msg = MessageBox.Show($"Do you want to save {Tabs.SelectedTab.Name}?", $"Save {Tabs.SelectedTab.Name}?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (msg == DialogResult.Yes)
                    {
                        ((nTabPage)Tabs.SelectedTab).saveNow();
                    }
                    else if (msg == DialogResult.Cancel)
                        return;
                }

                //delete local file
                string filename = "";
                if (((nTabPage)Tabs.SelectedTab).location == "")
                    filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).name));
                else
                    filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).location));
                System.IO.File.Delete(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename));

                int index = Tabs.SelectedIndex;
                var tab = Tabs.SelectedTab;
                if (Tabs.TabCount == 1)
                {
                    newTab();
                }
                Tabs.Controls.Remove(tab);

                if (index == 0)
                    Tabs.SelectedIndex = 0;
                else
                    Tabs.SelectedIndex = index - 1;
                ResetOrder();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                ((nTabPage)Tabs.SelectedTab).saveNow(true);
                ResetOrder();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                ((nTabPage)Tabs.SelectedTab).saveNow();
                ResetOrder();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                TxtMode = "searchAll";
                sTxt.Text = searchTxt;
                sTxt.Visible = true;
                sTxt.Focus();
                sTxt.SelectAll();
                searchText_TextChanged(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                TxtMode = "search";
                sTxt.Text = searchTxt;
                sTxt.Visible = true;
                sTxt.Focus();
                sTxt.SelectAll();
                searchText_TextChanged(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                ((nTabPage)Tabs.SelectedTab).Reload();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.E)
            {
                MenuCrypt_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Oemplus)
            {
                ((nTabPage)Tabs.SelectedTab).txtBox.Font = new Font(((nTabPage)Tabs.SelectedTab).txtBox.Font.FontFamily, ((nTabPage)Tabs.SelectedTab).txtBox.Font.Size + 1);
            }
            else if (e.Control && e.KeyCode == Keys.OemMinus)
            {
                ((nTabPage)Tabs.SelectedTab).txtBox.Font = new Font(((nTabPage)Tabs.SelectedTab).txtBox.Font.FontFamily, ((nTabPage)Tabs.SelectedTab).txtBox.Font.Size - 1);
            }
        }

        private void newTab(bool tmpLoad = false, string name = "", string location = "", bool isSaved = true, bool isEncrypted = false)
        {
            #region Right Click Menu for TextBox
            var contextMenu = new ContextMenuStrip();
            contextMenu.SuspendLayout();
            contextMenu.ShowImageMargin = false;
            contextMenu.BackColor = Color.Black;
            contextMenu.ForeColor = Color.White;

            ToolStripMenuItem MenuSaveAs = new ToolStripMenuItem();
            MenuSaveAs.Text = "SaveAs";
            MenuSaveAs.BackColor = Color.Black;
            MenuSaveAs.Click += MenuSaveAs_Click;

            ToolStripMenuItem MenuCopyFilePath = new ToolStripMenuItem();
            MenuCopyFilePath.Text = "Copy filepath";
            MenuCopyFilePath.Click += MenuCopyFilePath_Click;

            ToolStripMenuItem MenuCrypt = new ToolStripMenuItem();
            MenuCrypt.Name = "crypt";
            MenuCrypt.Text = "En/ Decrypt Tab";
            MenuCrypt.Click += MenuCrypt_Click;

            #region colors
            ToolStripMenuItem ColorWhite = new ToolStripMenuItem();
            ColorWhite.Text = "White";
            ColorWhite.Click += this.ColorWhite;
            ToolStripMenuItem ColorRed = new ToolStripMenuItem();
            ColorRed.Text = "Red";
            ColorRed.Click += this.ColorRed;
            ToolStripMenuItem ColorGreen = new ToolStripMenuItem();
            ColorGreen.Text = "Green";
            ColorGreen.Click += this.ColorGreen;
            ToolStripMenuItem ColorYellow = new ToolStripMenuItem();
            ColorYellow.Text = "Yellow";
            ColorYellow.Click += this.ColorYellow;
            ToolStripMenuItem ColorBlack = new ToolStripMenuItem();
            ColorBlack.Text = "Black";
            ColorBlack.Click += this.ColorBlack;
            var MenuColorDropDown = new ToolStripDropDown();
            MenuColorDropDown.AutoClose = true;

            MenuColorDropDown.Items.AddRange(new ToolStripItem[]
            {
                ColorWhite, ColorRed, ColorGreen, ColorYellow, ColorBlack
            });
            ToolStripDropDownItem MenuColor = new ToolStripMenuItem();
            MenuColor.Text = "Change color to";
            MenuColor.DropDown = MenuColorDropDown;
            #endregion

            //!!!

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                MenuSaveAs, MenuCrypt, MenuCopyFilePath, MenuColor
            });
            contextMenu.ResumeLayout(false);
            #endregion

            #region RichTextBox
            var txtBox = new RichTextBox();
            txtBox.Name = "txtBox";
            txtBox.BackColor = Color.Black;
            txtBox.ForeColor = Color.White;
            txtBox.Location = new Point(2, 0);
            txtBox.Size = new Size(400, 455);
            txtBox.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            txtBox.BorderStyle = BorderStyle.None;
            txtBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
            txtBox.AllowDrop = true;
            txtBox.TabStop = false;
            txtBox.AcceptsTab = true;
            txtBox.DragOver += TabsDragOver;
            txtBox.DragDrop += TabsDragDrop;
            txtBox.ContextMenuStrip = contextMenu;
            #endregion

            #region TabPage
            if (tmpLoad && (location == "" || !isSaved))
            {
                string filename;
                if (location == "")
                    filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                else
                    filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(location));
                byte[] buf = new byte[1024];
                int c;
                if (System.IO.File.Exists(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename)))
                {
                    using (FileStream fs = System.IO.File.OpenRead(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename)))
                    {
                        while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                        {
                            txtBox.AppendText(Encoding.ASCII.GetString(buf, 0, c));
                        }
                    }
                }
            }
            else if (location == "")
            {
                txtBox.Text = "\n\n\n\n\n\n\n\n\n\n";
                isSaved = false;
            }
            else
            {
                name = Path.GetFileName(location);
                byte[] buf = new byte[1024];
                int c;
                using (FileStream fs = System.IO.File.OpenRead(location))
                {
                    while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                    {
                        txtBox.AppendText(Encoding.ASCII.GetString(buf, 0, c));
                    }
                }
            }
            if (name == "")
            {
                int i = 0;
                for (int j = 0; j < Tabs.TabCount; j++)
                {
                    if (Tabs.TabPages[j].Name.Contains("new "))
                    {
                        try
                        {
                            if (Int16.Parse(Tabs.TabPages[j].Name.Replace("new ", "")) > i)
                                i = Int16.Parse(Tabs.TabPages[j].Name.Replace("new ", ""));
                        }
                        catch (Exception) { }
                    }
                }
                name = $"new {i + 1}";
            }
            var nTab = new nTabPage();
            nTab.name = name;
            nTab.location = location;
            nTab.saved = isSaved;
            nTab.enc = isEncrypted;
            nTab.BackColor = Color.Black;
            nTab.BorderStyle = BorderStyle.None;
            nTab.txtBox = txtBox;
            nTab.Controls.Add(txtBox);
            nTab.Text = name;
            nTab.Name = name;
            nTab.Padding = Padding.Empty;
            nTab.Init();
            #endregion

            
            Tabs.Controls.Add(nTab);
            Tabs.SelectTab(Tabs.TabCount - 1);
            ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
        }

        #region Color Text
        private void ColorBlack(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionColor = Color.Black;
        }

        private void ColorYellow(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionColor = Color.Yellow;
        }

        private void ColorGreen(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionColor = Color.Green;
        }

        private void ColorRed(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionColor = Color.Red;
        }

        private void ColorWhite(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionColor = Color.White;
        }
        #endregion

        private void MenuCrypt_Click(object sender, EventArgs e)
        {
            if (!((nTabPage)Tabs.SelectedTab).enc)
            {
                TxtMode = "Encrypt";
                sTxt.Text = "";
                sTxt.Visible = true;
                sTxt.Focus();
                sTxt.UseSystemPasswordChar = true;
                searchText_TextChanged(null, null);
            }
            else if (((nTabPage)Tabs.SelectedTab).decRn)
                ((nTabPage)Tabs.SelectedTab).EnCrypt();
            else if (!((nTabPage)Tabs.SelectedTab).decRn)
            {
                TxtMode = "Decrypt";
                sTxt.Text = "";
                sTxt.Visible = true;
                sTxt.Focus();
                sTxt.UseSystemPasswordChar = true;
                searchText_TextChanged(null, null);
            }
        }

        private void ResetOrder()
        {
            //create index file: name, location, isSaved, isEncrypted, isSelected
            if (!silent)
            {
                System.IO.File.Delete(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), "index"));
                using (var sw = new StreamWriter(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), "index"), false))
                {
                    for (int i = 0; i < Tabs.TabCount; i++)
                    {
                        sw.WriteLine($"{((nTabPage)Tabs.TabPages[i]).name}|||{((nTabPage)Tabs.TabPages[i]).location}|||{((nTabPage)Tabs.TabPages[i]).saved}|||{((nTabPage)Tabs.TabPages[i]).enc}|||{(Tabs.SelectedIndex == i)}");
                    }
                }
            }
        }

        private void MenuCopyFilePath_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(((nTabPage)Tabs.SelectedTab).location);
        }

        private void MenuSaveAs_Click(object sender, EventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).saveNow(true);
        }

        private void TabsDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }
        private void TabsDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[]; // get all files droppeds  
            foreach (string file in files)
                if (System.IO.File.Exists(file))
                {
                    bool neww = true;
                    for (int i = 0; i < Tabs.TabCount; i++)
                    {
                        if (((nTabPage)Tabs.TabPages[i]).location == file)
                        {
                            Tabs.SelectedTab = Tabs.TabPages[i];
                            neww = false;
                        }
                    }
                    if (neww)
                        newTab(false, "", file);
                }
        }

        private void Tabs_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.Tabs.DoDragDrop(this.Tabs.SelectedTab, DragDropEffects.All);
        }

        private void Tabs_DragDrop(object sender, DragEventArgs e)
        {
            TabPage drag_tab = (TabPage)e.Data.GetData(typeof(TabPage));
            int item_drag_index = Tabs.Controls.IndexOf(drag_tab);

            //Don't do anything if we are hovering over ourself.
            if (item_drag_index != 0)
            {
                ArrayList pages = new ArrayList();

                //Put all tab pages into an array.
                for (int i = 0; i < Tabs.TabPages.Count; i++)
                {
                    //Except the one we are dragging.
                    if (i != item_drag_index)
                        pages.Add(Tabs.TabPages[i]);
                }

                //Now put the one we are dragging it at the proper location.
                pages.Insert(0, drag_tab);

                //Make them all go away for a nanosec.
                Tabs.TabPages.Clear();

                //Add them all back in.
                Tabs.TabPages.AddRange((TabPage[])pages.ToArray(typeof(TabPage)));

                //Make sure the drag tab is selected.
                Tabs.SelectedTab = drag_tab;
                ResetOrder();
            }
        }

        private void Tabs_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TabPage)))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void searchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                sTxt.Visible = false;
                e.SuppressKeyPress = true;
                ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
                if (TxtMode == "search" || TxtMode == "searchAll")
                {
                    foreach (nTabPage page in Tabs.TabPages)
                    {
                        page.txtBox.SelectionStart = 0;
                        page.txtBox.SelectionLength = page.txtBox.TextLength;
                        page.txtBox.SelectionBackColor = Color.Black;
                    }
                }
                else if (TxtMode == "Encrypt" || TxtMode == "Decrypt")
                    sTxt.UseSystemPasswordChar = false;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (TxtMode == "search" || TxtMode == "searchAll")
                    searchTxt = sTxt.Text;
                sTxt.Visible = false;
                e.SuppressKeyPress = true;
                ((nTabPage)Tabs.SelectedTab).txtBox.Focus();
                searchText_TextChanged(null, null);
                if (TxtMode == "Encrypt" && !((nTabPage)Tabs.SelectedTab).enc)
                {
                    string filename = "";
                    if (((nTabPage)Tabs.SelectedTab).location == "")
                        filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).name));
                    else
                        filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(((nTabPage)Tabs.SelectedTab).location));
                    System.IO.File.Delete(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename));
                    ((nTabPage)Tabs.SelectedTab).EnCrypt(sTxt.Text);
                }
                else if (TxtMode == "Decrypt")
                    ((nTabPage)Tabs.SelectedTab).Decrypt(sTxt.Text);
                if (TxtMode == "Encrypt" || TxtMode == "Decrypt")
                    sTxt.UseSystemPasswordChar = false;
            }
        }

        private void searchText_TextChanged(object sender, EventArgs e)
        {
            if (TxtMode == "Encrypt" || TxtMode == "Decrypt")
                ;
            else if (TxtMode == "search" || TxtMode == "searchAll")
            {
                foreach (nTabPage page in Tabs.TabPages)
                {
                    page.txtBox.SelectionStart = 0;
                    page.txtBox.SelectionLength = page.txtBox.TextLength;
                    page.txtBox.SelectionBackColor = Color.Black;
                }
                if (sTxt.Text == "")
                {
                    return;
                }
                if (TxtMode == "searchAll")
                {
                    bool[] s = new bool[Tabs.TabCount];
                    foreach (nTabPage page in Tabs.TabPages)
                    {
                        page.txtBox.SelectionStart = 0;
                        page.txtBox.SelectionLength = page.txtBox.TextLength;
                        page.txtBox.SelectionBackColor = Color.Black;

                        int startIndex = 0;
                        while (startIndex < page.txtBox.TextLength)
                        {
                            //Find word & return index
                            int wordStartIndex = page.txtBox.Find(sTxt.Text, startIndex, RichTextBoxFinds.None);
                            if (wordStartIndex != -1)
                            {
                                //Highlight text in a richtextbox
                                page.txtBox.SelectionStart = wordStartIndex;
                                page.txtBox.SelectionLength = sTxt.Text.Length;
                                page.txtBox.SelectionBackColor = Color.Yellow;
                                s[Tabs.Controls.IndexOf(page)] = true;
                            }
                            else
                                break;
                            startIndex += wordStartIndex + sTxt.Text.Length;
                        }

                        if (!s[Tabs.Controls.IndexOf(Tabs.SelectedTab)])
                        {
                            int j = Tabs.Controls.IndexOf(Tabs.SelectedTab);
                            for (int i = 0; i < Tabs.TabCount - 1; i++)
                            {
                                j++;
                                if (j == Tabs.TabCount) j = 0;
                                if (s[j])
                                {
                                    Tabs.SelectedTab = Tabs.TabPages[j];
                                    sTxt.Focus();
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (TxtMode == "searchAll")
                {
                    int startIndex = 0;
                    while (startIndex < ((nTabPage)Tabs.SelectedTab).txtBox.TextLength)
                    {
                        //Find word & return index
                        int wordStartIndex = ((nTabPage)Tabs.SelectedTab).txtBox.Find(sTxt.Text, startIndex, RichTextBoxFinds.None);
                        if (wordStartIndex != -1)
                        {
                            //Highlight text in a richtextbox
                            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionStart = wordStartIndex;
                            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionLength = sTxt.Text.Length;
                            ((nTabPage)Tabs.SelectedTab).txtBox.SelectionBackColor = Color.Yellow;
                        }
                        else
                            break;
                        startIndex += wordStartIndex + sTxt.Text.Length;
                    }
                }
            }
        }
    }
}
