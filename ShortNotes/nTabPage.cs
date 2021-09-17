using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace ShortNotes
{
    public class nTabPage : TabPage
    {
        public string name;
        public bool saved = true;
        public string location = "";
        public RichTextBox txtBox;
        public bool enc = false;
        public bool decRn = false;
        private bool auto = false;
        private bool adding = false;
        protected string pw = "";

        private string tmpName;
        private BackgroundWorker backgroundWorker;
        private BackgroundWorker backgroundWorker2;

        public nTabPage()
        {
            this.AutoScroll = true;
            this.HScroll = true;
            this.VScroll = true;

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(this.backgroundWorker_DoWork);

            backgroundWorker2 = new BackgroundWorker();
            backgroundWorker2.WorkerSupportsCancellation = true;
            backgroundWorker2.DoWork += new DoWorkEventHandler(this.backgroundWorker2_DoWork);

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
            if (enc && decRn && auto)
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

                    byte[] bytes = Encoding.Unicode.GetBytes(text);
                    SymmetricAlgorithm crypt = Aes.Create();
                    HashAlgorithm hash = SHA256.Create();
                    crypt.BlockSize = 128;
                    crypt.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(this.pw));
                    crypt.IV = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream =
                           new CryptoStream(memoryStream, crypt.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(bytes, 0, bytes.Length);
                        }


                        using (FileStream outFile = File.Create(Path.Combine(Path.Combine(Application.StartupPath, "tmp"), filename)))
                        {
                            var bytess = Encoding.UTF8.GetBytes(Convert.ToBase64String(memoryStream.ToArray()));
                            outFile.Write(bytess, 0, bytess.Length);
                        }
                    }

                }
                catch (Exception) { }
            }
            else if (enc && decRn)
                ;
            else
            {
                if (saved && location != "" && !auto)
                {
                    try
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
                        }
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
                    }
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

        public void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            string text = "";
            MethodInvoker miReadText = new MethodInvoker(() =>
            {
                text = txtBox.Text;
            });
            this.Invoke(miReadText);

            string textrn = "";
            byte[] buf = new byte[1024];
            int c;
            using (FileStream fs = File.OpenRead(location))
            {
                while ((c = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    adding = true;
                    textrn += Encoding.ASCII.GetString(buf, 0, c);
                }
            }

            if (text == textrn) if (saved)
            {
                var msg = MessageBox.Show("This file been modified.\nDo you want to reload it?", "file has been modified", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (msg == DialogResult.Yes)
                {
                        Reload();
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
            if (enc && decRn)
            {
                var msg = MessageBox.Show($"The tab will encrypt before you can save it!", $"Encrypt?", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                if (msg != DialogResult.OK)
                    return;
                EnCrypt();
            }
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
        public void EnCrypt(string pw = "")
        {
            decRn = false;
            byte[] bytes = Encoding.Unicode.GetBytes(txtBox.Text);
            SymmetricAlgorithm crypt = Aes.Create();
            HashAlgorithm hash = SHA256.Create();
            crypt.BlockSize = 128;
            if (pw == "")
                crypt.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(this.pw));
            else
                crypt.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(pw));
            crypt.IV = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream =
                   new CryptoStream(memoryStream, crypt.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                }
                adding = true;
                txtBox.Text = Convert.ToBase64String(memoryStream.ToArray()); //Encrypted text is stored in Base64 String
            }

            if (!enc)
            {
                enc = true;
                startBackgroundWorker(true);
            }
            txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Decrypt Tab";

        }
        public void Decrypt(string pw = "")
        {
            string txt = txtBox.Text;
            try
            {
                decRn = true;
                this.pw = pw;
                byte[] bytes = Convert.FromBase64String(txtBox.Text); //Encrypted text is stored in Base64 String!
                SymmetricAlgorithm crypt = Aes.Create();
                HashAlgorithm hash = SHA256.Create();
                crypt.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(pw));
                crypt.IV = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    using (CryptoStream cryptoStream =
                       new CryptoStream(memoryStream, crypt.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        byte[] decryptedBytes = new byte[bytes.Length];
                        cryptoStream.Read(decryptedBytes, 0, decryptedBytes.Length);
                        adding = true;
                        txtBox.Text = Encoding.Unicode.GetString(decryptedBytes);
                    }
                }
                txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Encrypt Tab";
            }
            catch (Exception)
            {
                decRn = false;
                txtBox.Text = txt;
                this.pw = "";
                txtBox.ContextMenuStrip.Items.Find("crypt", true).First().Text = "Decrypt Tab";

            }
        }
    }
}
