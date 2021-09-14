using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShortNotes
{
    public class nTabPage : TabPage
    {
        public string name;
        public bool saved = true;
        public string location = "";
        public RichTextBox txtBox;
        public bool enc = false;
        private bool auto = false;
        private bool adding = false;

        private string tmpName;
        private BackgroundWorker backgroundWorker;
        public nTabPage()
        {
            this.AutoScroll = true;
            this.HScroll = true;
            this.VScroll = true;

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(this.backgroundWorker_DoWork);

        }

        public void Init()
        {
            txtBox.TextChanged += TxtBox_TextChanged;
            if (!saved)
                Text = name + "*";

        }

        public void startBackgroundWorker(bool autom = false)
        {
            auto = autom;
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
                try
                {
                    backgroundWorker.RunWorkerAsync();
                }
                catch (Exception) { }
            }
            else
                backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (enc)
            {
                //!!!
            }
            else
            {
                if (saved && location != "" && !auto)
                {
                    

                        string text = "";
                        MethodInvoker miReadText = new MethodInvoker(() =>
                        {
                            text = txtBox.Text;
                        });
                        this.Invoke(miReadText);

                        using (FileStream outFile = File.Create(location))
                        {
                            var bytes = Encoding.UTF8.GetBytes(text);
                            outFile.Write(bytes, 0, bytes.Length);
                        }/*
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Insufficient Permissions.\n Couldn't safe {name}!", "Insufficient Permissions", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        MethodInvoker mI = new MethodInvoker(() =>
                        {
                            saved = false;
                            name = tmpName;
                            Name = tmpName;
                            Text = name + "*";
                        });
                        this.Invoke(mI);
                    }*/
                }
                else if (saved && auto) { }
                else
                {
                    string filename = "";

                    filename = name;

                    if (location == "")
                        filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                    else
                        filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(location));

                    try
                    {
                        string text = "";
                        MethodInvoker miReadText = new MethodInvoker(() =>
                        {
                            text = txtBox.Text;
                        });
                        this.Invoke(miReadText);

                        using (FileStream outFile = File.Create(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename)))
                        {
                            var bytes = Encoding.UTF8.GetBytes(text);
                            outFile.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private void TxtBox_TextChanged(object sender, EventArgs e)
        {
            if (adding)
            {
                adding = false;
                return;
            }
            saved = false;
            Text = name + "*";
        }
        public void saveNow(bool safeAs = false)
        {
            if (location == "" || safeAs)
            {
                using (SaveFileDialog file = new SaveFileDialog())
                {
                    file.FileName = name;
                    file.DefaultExt = "txt";
                    file.OverwritePrompt = true;
                    if (file.ShowDialog() == DialogResult.OK)
                    {
                        location = file.FileName;
                        name = Path.GetFileName(file.FileName);
                        saved = true;
                        tmpName = name;
                        Text = name;
                        Name = name;
                        startBackgroundWorker();
                    }
                    else
                        return;
                }
            }
            else
            {
                tmpName = name;
                saved = true;
                Text = name;
                startBackgroundWorker();
            }
        }
        public void Reload()
        {
            if (location == "")
                return;
            if (!File.Exists(location))
                return; //!!! ask 
            string l = location;
            if (!saved)
            {
                var msg = MessageBox.Show("There are unsaved changes. Do you want to save to file before reloading?", "save before reload file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (msg == DialogResult.Yes)
                {
                    saveNow(true);
                    if (!saved)
                        return;
                }
                else if (msg == DialogResult.Cancel)
                    return;
            }
            while (backgroundWorker.IsBusy)
                Application.DoEvents();
            adding = true;
            txtBox.Clear();
            byte[] buf = new byte[1024];
            int c;
            using (FileStream fs = File.OpenRead(l))
            {
                while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    adding = true;
                    txtBox.AppendText(Encoding.ASCII.GetString(buf, 0, c));
                }
            }
            location = l;
            name = Path.GetFileName(l);
            saved = true;
            Text = name;
            Name = name;
        }
    }
}
