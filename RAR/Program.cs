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

            // Handle command line arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "compress" && args.Length > 2)
            {
                // If launched from context menu, show main form with file pre-selected
                MainForm mainForm = new MainForm();
                Application.Run(mainForm);
            }
            else
            {
                // Normal launch
                Application.Run(new MainForm());
            }
        }
    }
}