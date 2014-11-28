using System;
using System.Windows.Forms;
using NLog;

namespace NLogTest
{
    public partial class TestForm : Form
    {
        // We'll define a category here based on the type of this form: NLogTest.TestForm.
        private static readonly Logger m_Log = LogManager.GetCurrentClassLogger(); // Cache this for convenience.

        public TestForm()
        {
            m_Log.Info("Entering TestForm.");
            InitializeComponent();
        }

        private void ActionLogException()
        {
            try
            {
                try
                {
                    var badException = new ArgumentNullException("key", "this is the innermost exception");
                    throw badException;
                }
                catch (Exception ex)
                {
                    var outerException = new InvalidOperationException("This is the outer exception", ex);                    
                    throw outerException;
                }
            }
            catch (Exception ex)
            {
                m_Log.ErrorException("Oh Snap!  We just had an exception!", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            m_Log.Info("Exiting TestForm.");
        }

        private void BusyWorker_Click(object sender, EventArgs e)
        {
            string workerName = ((Button)sender).Text;
            BusyWork worker = new BusyWork(workerName);
            try
            {
                worker.Run(10);
            }
            catch (Exception ex)
            {
                //m_Log.WarnException("Worker error", ex); // Simple non-format method to log ex to NLog in general.
                m_Log.Warn("Worker error\r\nWorker {0} threw an exception.\r\n", workerName, ex); // Gibraltar will include ex from parameters in this case.
            }
        }

        private void btnLogException_Click(object sender, EventArgs e)
        {
            ActionLogException();
        }
    }
}