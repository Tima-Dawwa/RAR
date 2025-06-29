using RAR.UI;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RAR
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "compress" && args.Length > 2)
            {
                MainForm mainForm = new MainForm();
                Application.Run(mainForm);
            }
            else
            {
                Application.Run(new MainForm());
            }
        }
    }
}