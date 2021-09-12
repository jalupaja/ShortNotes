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

namespace ShortNotes
{
    public partial class Form1 : Form
    {
        public bool close = false;
        public bool onlyTray = false;
        public bool clean = false;
        public bool silent = false;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ((nTabPage)Tabs.SelectedTab).startBackgroundWorker();
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
        }

        KeyboardHook hook = new KeyboardHook(); //https://stackoverflow.com/questions/2450373/set-global-hotkeys-using-c-sharp

        NotifyIcon TrayIcon = new NotifyIcon();
        ContextMenuStrip TrayIconContextMenu = new ContextMenuStrip();
        ToolStripMenuItem MenuItemExit = new ToolStripMenuItem();
        ToolStripMenuItem MenuItemShow = new ToolStripMenuItem();

        ToolStripMenuItem AlwaysOnTop = new ToolStripMenuItem();

        private int lastTabIndex;

        public Form1()
        {
            InitializeComponent();

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
            Tabs.Padding = new Point(0, 0);

            if (!clean)
            {
                //!!! load last files here
            }
            else if (!silent)
            {
                foreach (string file in Directory.GetFiles(Path.Combine(Application.StartupPath, "tmp")))
                    File.Delete(file);
            }

            if (Tabs.TabCount == 0)
            {
                newTab();
            }

            lastTabIndex = Tabs.Controls.IndexOf(Tabs.SelectedTab);

            this.KeyPreview = true;

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
            TrayShow(null, null);
        }


        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!((nTabPage)Tabs.SelectedTab).saved || !silent) { ((nTabPage)Tabs.TabPages[lastTabIndex]).startBackgroundWorker(); }

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
                if (!((nTabPage)Tabs.SelectedTab).saved)
                {
                    var msg = MessageBox.Show($"Do you want to save {Tabs.SelectedTab.Name}?", $"Save {Tabs.SelectedTab.Name}?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (msg == DialogResult.OK)
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
                File.Delete(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename));

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
                //!!!
            }
            else if (e.Control && e.KeyCode == Keys.F) 
            { 
                //!!!
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

            ToolStripMenuItem MenuSaveAs = new ToolStripMenuItem();
            MenuSaveAs.Text = "SaveAs";
            MenuSaveAs.BackColor = Color.Black;
            MenuSaveAs.Click += MenuSaveAs_Click;

            ToolStripMenuItem MenuCopyFilePath = new ToolStripMenuItem();
            MenuCopyFilePath.Text = "Copy filepath";
            MenuCopyFilePath.Click += MenuCopyFilePath_Click;

            //!!!

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                MenuSaveAs, MenuCopyFilePath
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
            if (location == "")
                txtBox.Text = "\n\n\n\n\n\n\n\n\n\n";
            else
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
            Tabs.SelectedTab.Controls.Find("txtBox", false).First().Focus();
        }

        private void ResetOrder()
        {
            //!!! create index file: name, location, enc
            if (!silent)
            {
                
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
                if (File.Exists(file))
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
                        newTab("", file);
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
    }
}
