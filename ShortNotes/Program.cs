using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShortNotes
{
    static class Program
    {
        static Form1 app = null;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            app = new Form1();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 )
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-s":
                            app.onlyTray = true;
                            break;
                        case "-c":
                            app.clean = true;
                            break;
                    }
            }

            Application.Run(app);
        }
    }
}
