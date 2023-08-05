using Eto.Forms;
using System;

namespace ZPLPrinter.Winform
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.WinForms.Platform();
            new Application(platform).Run(new MainForm());
        }
    }
}
