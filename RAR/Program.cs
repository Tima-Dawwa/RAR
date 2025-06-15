using RAR.Core.Compression;
using RAR.Helper;
using RAR.UI;
using System;
using System.IO;
using System.Windows.Forms;

namespace RAR
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}