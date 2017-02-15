using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace alexbegh.Utility.Helpers.Logging
{
    /// <summary>
    /// Log Level
    /// </summary>
    public enum SimpleLogLevel
    {
        /// <summary>
        /// Used for checkpoint logging
        /// </summary>
        Checkpoint,

        /// <summary>
        /// Log Level: Information
        /// </summary>
        Info,

        /// <summary>
        /// Log Level: Warning
        /// </summary>
        Warn,

        /// <summary>
        /// Log Level: Error
        /// </summary>
        Error
    }

    /// <summary>
    /// Simple logger class
    /// </summary>
    public static class SimpleLogger
    {
        [ThreadStatic]
        private static List<string> _lastCheckpoints;

        private static List<string> LastCheckpoints
        {
            get
            {
                if (_lastCheckpoints == null)
                    _lastCheckpoints = new List<string>();
                return _lastCheckpoints;
            }
        }

        private static object _lock = new object();

        static SimpleLogger()
        {
        }

        /// <summary>
        /// Initialize Logger, defaulting to calling assemblies' name
        /// </summary>
        /// <param name="logName"></param>
        public static void Init(string logName = null)
        {
            if (logName == null)
            {
                logName = Assembly.GetCallingAssembly().FullName;
            }
            
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create),
                Assembly.GetCallingAssembly().GetName().Name);

            try
            {
                Directory.CreateDirectory(basePath);
                try
                {
                    DirectoryInfo dInfo = new DirectoryInfo(basePath);
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                }
                catch(Exception)
                {
                }
            }
            catch(Exception ex)
            {
                LogFilePath = null;
                SimpleLogger.Log(ex);
                return;
            }

            LogFilePath = Path.Combine(basePath, logName + ".log");
        }

        /// <summary>
        /// Path to log file
        /// </summary>
        public static string LogFilePath
        {
            get;
            set;
        }

        private static StreamWriter _logWriter;

        /// <summary>
        /// Stream writer to the log file
        /// </summary>
        public static StreamWriter LogWriter
        {
            get
            {
                if (_logWriter == null)
                {
                    Exception ex = null;
                    try
                    {
                        if (LogFilePath != null)
                        {
                            var stream = new FileStream(LogFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                            stream.Seek(0, SeekOrigin.End);
                            _logWriter = new StreamWriter(stream, Encoding.UTF8, 4096);
                        }
                    }
                    catch (Exception fEx)
                    {
                        ex = fEx;
                    }

                    if (_logWriter==null)
                    {
                        LogFilePath = null;
                        _logWriter = new StreamWriter(new MemoryStream());
                        Log(ex);
                    }
                }
                return _logWriter;
            }
        }

        /// <summary>
        /// Sets a checkpoint entry for the current thread which will be logged at a later
        /// point in time if either desired (by calling LogCheckpoints) or an exception
        /// is thrown
        /// </summary>
        /// <param name="text">The text to log</param>
        public static void Checkpoint(string text)
        {
            LastCheckpoints.Insert(0,
                String.Format("{0} Checkpoint info: {1}",
                DateTime.Now.ToUniversalTime().ToString("[yyyyMMddHHmmss] ", CultureInfo.InvariantCulture.DateTimeFormat),
                text));
            if (LastCheckpoints.Count > 5000)
                LastCheckpoints.RemoveAt(5000);
        }

        /// <summary>
        /// Sets a checkpoint entry for the current thread which will be logged at a later
        /// point in time if either desired (by calling LogCheckpoints) or an exception
        /// is thrown
        /// </summary>
        /// <param name="format">The format string to log</param>
        /// <param name="args">The parameters</param>
        public static void Checkpoint(string format, params object[] args)
        {
            Checkpoint(String.Format(format, args));
        }

        /// <summary>
        /// Create a checkpoint with the current callers code info
        /// </summary>
        /// <param name="callingMethod">Calling method</param>
        /// <param name="callerFilePath">Calling file</param>
        /// <param name="callerLineNumber">Calling line no</param>
        public static void CheckpointDbg([CallerMemberName] string callingMethod = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            Checkpoint("[Dbg] Method: {0}, Path: {1}, Line: {2}", callingMethod, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// Logs the last n checkpoints
        /// </summary>
        /// <param name="count">Maximum no. of checkpoints (up to 5000)</param>
        public static void LogLastCheckpoints(int count = 0)
        {
            lock (_lock)
            {
                if (count==0)
                    count = LastCheckpoints.Count;
                foreach (var checkpoint in LastCheckpoints)
                {
                    if ((--count)<0)
                        break;
                    Log(SimpleLogLevel.Checkpoint, checkpoint);
                }
            }
        }

        /// <summary>
        /// Log text
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="text">Text to log</param>
        public static void Log(SimpleLogLevel level, string text)
        {
            lock (_lock)
            {
                LogWriter.Write(DateTime.Now.ToUniversalTime().ToString("[yyyyMMddHHmmss] ", CultureInfo.InvariantCulture.DateTimeFormat));
                LogWriter.Write("#{0:x8} ", System.Threading.Thread.CurrentThread.ManagedThreadId);
                switch (level)
                {
                    case SimpleLogLevel.Checkpoint:
                        LogWriter.Write("CHECK: ");
                        break;
                    case SimpleLogLevel.Info:
                        LogWriter.Write("INFO:  ");
                        break;
                    case SimpleLogLevel.Warn:
                        LogWriter.Write("WARN:  ");
                        break;
                    case SimpleLogLevel.Error:
                        LogWriter.Write("ERROR: ");
                        break;
                }
                bool isFirst = true;
                foreach (var line in text.Split('\n'))
                {
                    if (isFirst)
                    {
                        LogWriter.WriteLine(line);
                        isFirst = false;
                    }
                    else
                        LogWriter.WriteLine("                                  " + line);
                }
                LogWriter.Flush();
            }
        }

        /// <summary>
        /// Log text
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="text">Text to log</param>
        /// <param name="args">Format arguments</param>
        public static void Log(SimpleLogLevel level, string text, params object[] args)
        {
            Log(level, String.Format(text, args));
        }

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="ex">Exception to log</param>
        /// <param name="show">Show exception to user</param>
        /// <param name="checkpoints">Dump checkpoints also</param>
        public static void Log(Exception ex, bool show = true, bool checkpoints = true)
        {
            if (ex is OperationCanceledException)
                return;

            if (ex is AggregateException)
            {
                lock (_lock)
                {
                    Log(SimpleLogLevel.Error, "Multiple Exceptions occurred, listing each:");
                    foreach (var item in (ex as AggregateException).InnerExceptions)
                    {
                        Log(item, show, false);
                    }
                    Log(SimpleLogLevel.Error, "Multiple Exception Logging finished");
                    if (checkpoints)
                        LogLastCheckpoints();
                }
            }
            else
            {
                lock (_lock)
                {
                    Log(SimpleLogLevel.Error,
                        "Exception '{0}' occurred, Stack trace:\n{1}",
                        ex.Message,
                        ex.StackTrace);

                    if (checkpoints)
                        LogLastCheckpoints();

                    if (show)
                    {
                        MessageBox.Show(
                            String.Format("An exception occurred in vMerge\n\nDetail:\nException '{0}' occurred, Stacktrace:\n{1}", ex.Message, ex.StackTrace),
                            "vMerge Exception", MessageBoxButton.OK);
                    }
                }
            }
        }

        /// <summary>
        /// Reads from the end of the log file, at most 512k
        /// </summary>
        /// <returns>Log file contents</returns>
        public static string GetLogFileContents()
        {
            lock (_lock)
            {
                if (LogFilePath != null)
                {
                    _logWriter.Flush();
                    _logWriter.Close();
                    _logWriter.Dispose();
                    _logWriter = null;
                    using (var readStream = File.OpenRead(LogFilePath))
                    {
                        readStream.Seek(Math.Max(-524288, -readStream.Length), SeekOrigin.End);
                        using (var streamReader = new StreamReader(readStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    _logWriter.Flush();
                    var readStream = _logWriter.BaseStream;
                    readStream.Seek(Math.Max(-524288, -readStream.Length), SeekOrigin.End);
                    using (var streamReader = new StreamReader(readStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
