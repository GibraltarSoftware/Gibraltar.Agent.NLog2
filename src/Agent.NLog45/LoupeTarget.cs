using System;
using System.Collections;
using System.Collections.Generic;
using Gibraltar.Agent;
using Loupe.Configuration;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Loupe.Agent.NLog
{
    /// <summary>
    /// A specialized adapter Target for sending NLog event messages to Loupe's central log.
    /// </summary>
    [Target("Loupe")]
    public class LoupeTarget : TargetWithLayout
    {
        private const string ThisLogSystem = "NLog";

        /// <summary>
        /// Gets or sets a value indicating whether to include source info (file name and line number)
        /// </summary>
        public bool IncludeCallSite { get; set; }

        /// <summary>
        /// Include <see cref="LogEventInfo.Properties"/> in MessageDetails
        /// </summary>
        public bool IncludeEventProperties
        {
            get => GetMessageDetailsLayout(false)?.IncludeAllProperties ?? false;
            set
            {
                var messageDetails = GetMessageDetailsLayout(value);
                if (messageDetails != null)
                    messageDetails.IncludeAllProperties = value;
            }
        }

        /// <summary>
        /// Include <see cref="MappedDiagnosticsLogicalContext"/> in MessageDetails
        /// </summary>
        /// <remarks>
        /// Will include scope properties from Microsoft-Extension-Logging ILogger.BeginScope
        /// </remarks>
        public bool IncludeMldc
        {
            get => GetMessageDetailsLayout(false)?.IncludeMdlc ?? false;
            set
            {
                var messageDetails = GetMessageDetailsLayout(value);
                if (messageDetails != null)
                    messageDetails.IncludeMdlc = value;
            }
        }

        /// <summary>
        /// Include additional context properties in MessageDetails
        /// </summary>
        [ArrayParameter(typeof(JsonAttribute), "contextproperty")]
        public IList<JsonAttribute> ContextProperties
        {
            get => new ContextPropetiesProxy(this);
            set
            {
                var messageDetails = GetMessageDetailsLayout(value?.Count > 0);
                if (messageDetails != null)
                {
                    messageDetails.Attributes.Clear();
                    foreach (var attribute in value)
                        messageDetails.Attributes.Add(attribute);
                }
            }
        }

        /// <summary>
        /// Loupe already captures the class name, so the convention of using the logging class as the logger name
        /// would mean that the default "category" may be redundant information.  If you would like to capture some other
        /// property from the LogEventInfo as the log event's category (a dot-delimited hierarchy, similar to a namespace)
        /// then you can add that code here.
        /// </summary>
        public Layout Category { get; set; }

        /// <summary>
        /// Extended message details formatted as JSON (or XML)
        /// </summary>
        public Layout MessageDetails { get; set; }

        /// <summary>
        /// A simple single-line message caption. (Will not be processed for formatting.)
        /// </summary>
        public Layout Caption { get; set; }

        /// <summary>
        /// Internal property for signalling the NLog Layout engine to capture CallSite-information
        /// </summary>
        public Layout EnableCallsiteLayout => IncludeCallSite ? _enableCallsiteLayout : null;
        private Layout _enableCallsiteLayout = new SimpleLayout("${callsite}");

        /// <summary>
        /// Initializes a new instance of the <see cref="LoupeTarget" /> class.
        /// </summary>
        public LoupeTarget()
        {
            Layout = "${message}";  // LogEventInfo.FormattedMessage
            Category = "${logger}";
            IncludeCallSite = true;
            OptimizeBufferReuse = true; // Optimize performance of RenderLogEvent()
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoupeTarget" /> class.
        /// </summary>
        public LoupeTarget(string name, AgentConfiguration agentConfiguration)
            : this()
        {
            Name = name;
            Gibraltar.Agent.Log.StartSession(agentConfiguration); // We want to be sure the session has started. This is safe to call repeatedly.
        }

        /// <summary>
        /// Write the log event received by this Target into the Loupe central log.
        /// </summary>
        /// <param name="logEvent">The LogEventInfo for a log event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
                return;

            var formattedMessage = RenderLogEvent(Layout, logEvent);
            if (string.IsNullOrEmpty(formattedMessage))
                formattedMessage = logEvent.FormattedMessage;

            var category = RenderLogEvent(Category, logEvent);
            if (string.IsNullOrEmpty(category))
                category = logEvent.LoggerName;

            var severity = GetSeverityLevel(logEvent.Level);
            var sourceProvider = IncludeCallSite ? NLogSourceProvider.Create(logEvent) : NLogSourceProvider.NoMessageSource;
            var exception = GetException(logEvent);

            var messageCaption = RenderLogEvent(Caption, logEvent);
            if (string.IsNullOrEmpty(messageCaption))
                messageCaption = null;

            var messageDetails = RenderLogEvent(MessageDetails, logEvent);
            if (string.IsNullOrEmpty(messageDetails))
                messageDetails = null;

            // By passing null for caption it will automatically get the caption from the first line of the formatted message.
            Gibraltar.Agent.Log.Write(severity, ThisLogSystem, sourceProvider, null, exception, LogWriteMode.Queued,
                                      messageDetails, category, messageCaption, formattedMessage);
        }

        JsonLayout GetMessageDetailsLayout(bool allocate)
        {
            if (MessageDetails == null && allocate)
                MessageDetails = new JsonLayout() { MaxRecursionLimit = 1 };
            return MessageDetails as JsonLayout;
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
            public static readonly IMessageSourceProvider NoMessageSource = new NLogSourceProvider();

            public static IMessageSourceProvider Create(LogEventInfo logEvent)
            {
                try
                {
                    var methodName = logEvent.CallerMemberName;
                    if (string.IsNullOrEmpty(methodName))
                        methodName = null;
                    var className = logEvent.CallerClassName;
                    if (string.IsNullOrEmpty(className))
                        className = null;
                    var fileName = logEvent.CallerFilePath;
                    if (string.IsNullOrEmpty(fileName))
                        fileName = null;

                    if (methodName != null || className != null || fileName != null)
                    {
                        if (string.IsNullOrEmpty(className))
                            className = logEvent.LoggerName;
                        var lineNumber = logEvent.CallerLineNumber;
                        return new NLogSourceProvider() { ClassName = className, MethodName = methodName, FileName = fileName, LineNumber = lineNumber };
                    }
                }
                catch
                {
                    // No message source available
                }

                return NoMessageSource;
            }

            /// <summary>
            /// Should return the simple name of the method which issued the log message.
            /// </summary>
            public string MethodName { get; private set; }

            /// <summary>
            /// Should return the full name of the class (with namespace) whose method issued the log message.
            /// </summary>
            public string ClassName { get; private set; }

            /// <summary>
            /// Should return the name of the file containing the method which issued the log message.
            /// </summary>
            public string FileName { get; private set; }

            /// <summary>
            /// Should return the line within the file at which the log message was issued.
            /// </summary>
            public int LineNumber { get; private set; }
        }

        /// <summary>
        /// IList-Proxy for ContextProperties to skip allocation and rendering using JsonLayout unless requested
        /// </summary>
        private class ContextPropetiesProxy : IList<JsonAttribute>, IList
        {
            private readonly LoupeTarget _loupeTarget;
            IList<JsonAttribute> _contextProperties;

            public ContextPropetiesProxy(LoupeTarget loupeTarget)
            {
                _loupeTarget = loupeTarget;
                _contextProperties = GetContextProperties(false);
            }

            private IList<JsonAttribute> GetContextProperties(bool allocate = true)
            {
                return _contextProperties = _loupeTarget.GetMessageDetailsLayout(allocate)?.Attributes ?? Array.Empty<JsonAttribute>();
            }

            public JsonAttribute this[int index] { get => _contextProperties[index]; set => _contextProperties[index] = value; }
            object IList.this[int index] { get => _contextProperties[index]; set => _contextProperties[index] = (JsonAttribute)value; }

            public int Count => _contextProperties.Count;

            public bool IsReadOnly => _contextProperties.IsReadOnly;

            bool IList.IsFixedSize => false;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => true;

            public void Add(JsonAttribute item)
            {
                GetContextProperties()?.Add(item);
            }

            public void Insert(int index, JsonAttribute item)
            {
                GetContextProperties()?.Insert(index, item);
            }

            public bool Remove(JsonAttribute item)
            {
                return GetContextProperties()?.Remove(item) ?? false;
            }

            public void RemoveAt(int index)
            {
                GetContextProperties()?.RemoveAt(index);
            }

            public void Clear()
            {
                if (_contextProperties?.Count > 0)
                    _contextProperties.Clear();
            }

            public IEnumerator<JsonAttribute> GetEnumerator() => _contextProperties.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_contextProperties).GetEnumerator();

            int IList.Add(object value)
            {
                Add((JsonAttribute)value);
                return Count - 1;
            }

            public bool Contains(JsonAttribute item) => _contextProperties.Contains(item);
            public void CopyTo(JsonAttribute[] array, int arrayIndex) => _contextProperties.CopyTo(array, arrayIndex);
            public int IndexOf(JsonAttribute item) => _contextProperties.IndexOf(item);

            bool IList.Contains(object value) => _contextProperties.Contains((JsonAttribute)value);
            int IList.IndexOf(object value) => _contextProperties.IndexOf((JsonAttribute)value);
            void IList.Insert(int index, object value) => _contextProperties.Insert(index, (JsonAttribute)value);
            void IList.Remove(object value) => _contextProperties.Remove((JsonAttribute)value);
            void ICollection.CopyTo(Array array, int index) => _contextProperties.CopyTo((JsonAttribute[])array, index);
        }
    }
}
