using System;
using System.Diagnostics;
using System.Reflection;
using Gibraltar.Agent;
using NLog;
using NLog.Layouts;
using NLog.Targets;

namespace Loupe.Agent.NLog
{
    /// <summary>
    /// A specialized adapter Target for sending NLog event messages to Loupe's central log.
    /// </summary>
    [Target("Gibraltar")]
    public class GibraltarTarget : TargetWithLayout
    {
        private const string ThisLogSystem = "NLog";

        public GibraltarTarget()
        {
            Layout = new SimpleLayout("${callsite}"); //just to force NLog to include stack trace info.
        }

        /// <summary>
        /// Write the log event received by this Target into the Loupe central log.
        /// </summary>
        /// <param name="logEvent">The LogEventInfo for a log event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
                return;

            var formattedMessage = Layout.Render(logEvent);

            var severity = GetSeverityLevel(logEvent.Level);
            var sourceProvider = new NLogSourceProvider(logEvent);
            var category = GetCategory(logEvent);
            var exception = GetException(logEvent);

            // By passing null for caption it will automatically get the caption from the first line of the formatted message.
            Gibraltar.Agent.Log.Write(severity, ThisLogSystem, sourceProvider, null, exception, LogWriteMode.Queued,
                                      null, category, null, logEvent.FormattedMessage);
        }

        /// <summary>
        /// A helper method to determine the "category" for a log event when sending it to Loupe.
        /// </summary>
        /// <param name="logEvent">The LogEventInfo for a log event.</param>
        /// <returns>A dot-delimited hierarchy string similar to a namespace. (By default just returns the LoggerName.)</returns>
        private static string GetCategory(LogEventInfo logEvent)
        {
            string category = logEvent.LoggerName; // Default to just the logger name, but this is normally just the class name.

            /*
             * Loupe already captures the class name, so the convention of using the logging class as the logger name
             * would mean that the default "category" may be redundant information.  If you would like to capture some other
             * property from the LogEventInfo as the log event's category (a dot-delimited hierarchy, similar to a namespace)
             * then you can add that code here.
             */

            return category;
        }

        /// <summary>
        /// A helper method to find any Exception provided with the log event.
        /// </summary>
        /// <remarks>Loupe allows an Exception to be attached to any log message, but the NLog methods for logging
        /// exceptions don't support format parameters (only a simple string).  This logic will allow any Exception passed
        /// among the format parameters of a log event (even one ignored by the format string) to be specified to Loupe,
        /// so you can log an Exception to Loupe with a formatted message.  However, only the first Exception among the
        /// format parameters will be attached to the message in Loupe, and other NLog targets will not necessarily
        /// act on an Exception included among the format parameters, beyond the normal format expansion.</remarks>
        /// <param name="logEvent">The LogEventInfo for a log event.</param>
        /// <returns>The Exception specified in the LogEventInfo, or else the first Exception found among the Parameters.</returns>
        private static Exception GetException(LogEventInfo logEvent)
        {
            Exception exception = logEvent.Exception;
            object[] parameters = logEvent.Parameters;

            // If an Exception wasn't already specified, see if an Exception is among the format parameters.
            if (exception == null && parameters != null && parameters.Length > 0)
            {
                foreach (object parameter in parameters)
                {
                    Exception exceptionParameter = parameter as Exception;
                    if (exceptionParameter != null)
                    {
                        exception = exceptionParameter;
                        break; // Exit after finding first one.
                    }
                }
            }

            return exception;
        }

        /// <summary>
        /// A helper method to translate an NLog's log event level into a Loupe log message severity.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static LogMessageSeverity GetSeverityLevel(LogLevel level)
        {
            LogMessageSeverity severity = LogMessageSeverity.None;

            // Check from most-frequent first, but round any unrecognized levels down to the best Loupe severity match.
            if (level < LogLevel.Info)
                severity = LogMessageSeverity.Verbose;
            else if (level < LogLevel.Warn)
                severity = LogMessageSeverity.Information;
            else if (level < LogLevel.Error)
                severity = LogMessageSeverity.Warning;
            else if (level < LogLevel.Fatal)
                severity = LogMessageSeverity.Error;
            else if (level < LogLevel.Off)
                severity = LogMessageSeverity.Critical;

            return severity;
        }

        /// <summary>
        /// A conversion class to extract full message source information from LogEventInfo and provide it for Loupe.
        /// </summary>
        private class NLogSourceProvider : IMessageSourceProvider
        {
            /// <summary>
            /// Construct an NLogSourceProvider for a given log event.
            /// </summary>
            /// <param name="logEvent">The LogEventInfo of the log event.</param>
            public NLogSourceProvider(LogEventInfo logEvent)
            {
                MethodName = null;
                ClassName = null;
                FileName = null;
                LineNumber = 0;

                try
                {
                    StackFrame frame = logEvent.UserStackFrame;
                    if (frame != null)
                    {
                        MethodBase method = frame.GetMethod();
                        if (method != null)
                        {
                            MethodName = method.Name;
                            ClassName = method.ReflectedType.FullName;

                            try
                            {
                                // Now see if we also have file information.
                                FileName = frame.GetFileName();
                                if (string.IsNullOrEmpty(FileName) == false)
                                {
                                    LineNumber = frame.GetFileLineNumber();
                                }
                                else
                                {
                                    LineNumber = 0; // Not meaningful if there's no file name!
                                }
                            }
                            catch
                            {
                                FileName = null;
                                LineNumber = 0;
                            }
                        }
                    }
                }
                catch
                {
                    MethodName = null;
                    ClassName = null;
                    FileName = null;
                    LineNumber = 0;
                }
            }

            /// <summary>
            /// Should return the simple name of the method which issued the log message.
            /// </summary>
            public string MethodName { get; }

            /// <summary>
            /// Should return the full name of the class (with namespace) whose method issued the log message.
            /// </summary>
            public string ClassName { get; }

            /// <summary>
            /// Should return the name of the file containing the method which issued the log message.
            /// </summary>
            public string FileName { get; }

            /// <summary>
            /// Should return the line within the file at which the log message was issued.
            /// </summary>
            public int LineNumber { get; }
        }
    }
}
