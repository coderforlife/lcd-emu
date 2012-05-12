using System;
using System.Windows.Forms;
using System.Xml;

namespace LCD.Emulator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            XmlDocument doc = new XmlDocument();
            doc.Load("settings.xml");
            LCDChip lcd = new LCDChip(doc["LCD"]);
        }
    }
}
