using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShortNotes
{
    public partial class Form1 : Form
    {
        public bool close = false;
        public bool onlyTray = false;

        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated) CreateHandle();
            base.SetVisibleCore(!onlyTray);
        }

        NotifyIcon TrayIcon = new NotifyIcon();
        ContextMenuStrip TrayIconContextMenu = new ContextMenuStrip();
        ToolStripMenuItem MenuItemExit = new ToolStripMenuItem();
        ToolStripMenuItem MenuItemShow = new ToolStripMenuItem();

        ToolStripMenuItem AlwaysOnTop = new ToolStripMenuItem();
        ToolStripMenuItem MenuCopyFilePath = new ToolStripMenuItem();

        public Form1()
        {
            InitializeComponent();

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

            //!!!

            contextMenuTop.Items.AddRange(new ToolStripItem[]
            {
                AlwaysOnTop
            });
            contextMenuTop.ResumeLayout(false);
            this.ContextMenuStrip = contextMenuTop;
            #endregion

            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);

            if (Tabs.TabCount == 0)
            {
                newTab();
            }

            this.KeyPreview = true;
        }

        private void AlwaysOnTop_Click(object sender, EventArgs e)
        {
            if(AlwaysOnTop.Text.Contains("off"))
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

        private void TrayShow(object sender, EventArgs e)
        {
            if (onlyTray)
            {
                onlyTray = false;
                base.SetVisibleCore(true);
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
                MenuItemShow.Text = "Hide";
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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

        #region move and resize stuff by: https://stackoverflow.com/questions/2575216/how-to-move-and-resize-a-form-without-a-border
        private const int cGrip = 18;      // Grip size
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
            TrayIcon_Click(null, null);
        }

        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            Tabs.SelectedTab.Controls.Find("txtBox", false).First().Focus();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Tab)
            {
                //!!! smart tabs
                if (Tabs.Controls.IndexOf(Tabs.SelectedTab) == Tabs.TabCount-1)
                    Tabs.SelectTab(0);
                else
                    Tabs.SelectTab(Tabs.Controls.IndexOf(Tabs.SelectedTab) + 1);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.N || e.KeyCode == Keys.T))
            {
                newTab();
                e.SuppressKeyPress = true;
            }
            else  if (e.Control && e.KeyCode == Keys.W)
            {
                //!!! ask if wants to safe
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
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                //!!! safe or open "safe to"
                e.SuppressKeyPress = true;
            }
        }

        private void newTab(string name = "", string location = "")
        {
            #region Right Click Menu for TextBox
            var contextMenu = new ContextMenuStrip();
            contextMenu.SuspendLayout();
            contextMenu.ShowImageMargin = false;
            contextMenu.BackColor = Color.Black;
            contextMenu.ForeColor = Color.White;

            ToolStripMenuItem MenuSafeAs = new ToolStripMenuItem();
            MenuSafeAs.Text = "SafeAs";
            MenuSafeAs.BackColor = Color.Black;
            MenuSafeAs.Click += MenuSafeAs_Click;

            ToolStripMenuItem MenuCopyFilePath = new ToolStripMenuItem();
            MenuCopyFilePath.Text = "Copy filepath";
            MenuCopyFilePath.Click += MenuCopyFilePath_Click;

            //!!!

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                MenuSafeAs, MenuCopyFilePath
            });
            contextMenu.ResumeLayout(false);
            #endregion

            #region RichTextBox
            var txtBox = new RichTextBox();
            txtBox.Name = "txtBox";
            txtBox.BackColor = Color.Black;
            txtBox.ForeColor = Color.White;
            txtBox.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left);
            txtBox.BorderStyle = BorderStyle.None;
            txtBox.ScrollBars = RichTextBoxScrollBars.Both;
            txtBox.AllowDrop = true;
            txtBox.TabStop = false;
            txtBox.AcceptsTab = true;
            txtBox.DragOver += TabsDragOver;
            txtBox.DragDrop += TabsDragDrop;
            txtBox.ContextMenuStrip = contextMenu;
            #endregion

            #region TabPage
            if (location != "")
            {
                name = Path.GetFileName(location);
                byte[] buf = new byte[1024];
                int c;
                using (FileStream fs = File.OpenRead(location))
                {
                    while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                    {
                        txtBox.AppendText(Encoding.ASCII.GetString(buf, 0, c));
                    }
                }
            }
            if (name == "")
            {
                int i = 1;
                name = $"new {i}";
                while (false)//!!! while name doesnt exist
                {
                    i++;
                    name = $"new {i}";
                }
            }
            var nTab = new TabPage();
            nTab.BackColor = Color.Black;
            nTab.BorderStyle = BorderStyle.None;
            nTab.Controls.Add(txtBox);
            nTab.Text = name;
            #endregion

            Tabs.Controls.Add(nTab);
            Tabs.SelectTab(Tabs.TabCount-1);
            Tabs.SelectedTab.Controls.Find("txtBox", false).First().Focus();
        }

        private void MenuCopyFilePath_Click(object sender, EventArgs e)
        {
            //!!!
        }

        private void MenuSafeAs_Click(object sender, EventArgs e)
        {
            //!!!
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
                if (File.Exists(file)) { newTab("", file); }
        }
    }
}
