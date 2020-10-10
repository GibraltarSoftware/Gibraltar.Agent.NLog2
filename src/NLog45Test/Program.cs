using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gibraltar.Agent;
using NLog;

namespace NLogTest
{
    static class Program
    {
        // NLog normally uses the class as the logger name, but Loupe already includes class and method information
        // of the issuer.  So for this demo app we'll use a different logical hierarchy for the logger names, which will
        // become the category string within Loupe.  Loupe is added through the NLog configuration in app.config.
        private static readonly Logger log = LogManager.GetLogger("Application"); // Since we only use one category here...

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                log.Info("Starting application.");
                Application.Run(new TestForm());
            }
            finally
            {
                Log.EndSession("Application shutting down");
            }
        }
    }
}