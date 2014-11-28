using System;
using System.Threading;
using NLog;

namespace NLogTest
{
    public class BusyWork
    {
        private readonly string m_Name;
        private readonly string m_Category;
        private readonly Logger m_Log; // And cache this for convenience.

        public BusyWork(string name)
        {
            m_Name = name;
            // Create a category by combining info from the type with additional info
            m_Category = typeof(TestForm).FullName + "." + name;
            m_Log = LogManager.GetLogger(m_Category);
        }

        public void Run(int messages)
        {
            // Queue the task and data.
            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), messages))
                m_Log.Warn("QueueUserWorkItem failed");

            if (m_Name == "Shemp")
                throw new ApplicationException("This is a test exception");
        }

        private void ThreadProc(object info)
        {
            int count = (int)info;

            for (int i = 1; i <= count; i++)
            {
                m_Log.Trace("{0} message {1} of {2}", m_Name, i, count);
                Thread.Sleep(500);
            }
        }
    }
}