using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MBM.Classes
{
    public static class Logging
    {
        internal static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger(); // Global error logging object

        /// <summary>
        /// Log the passed error message to the system and display a message before terminating the application if required
        /// </summary>
        internal static void LogEvent(string message, bool closeApplication = true)
        {
            try
            {
                File.AppendAllLines(App.logFilePath, new[] { message });
                LogSystemEvent($"Money-B-Mine: {message}"); // Save system event
            }
            finally
            {
                MessageBox.Show("An Unknown Error Occured. Please contact the development team.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (closeApplication == true)
                {
                    Application.Current.Shutdown(); // exit the application
                }
            }
        }

        /// <summary>
        /// Override - Log the passed error message to the system and display passed message before terminating the application if required
        /// </summary>
        internal static void LogEvent(string logMessage, string displayMessage, bool closeApplication = true)
        {
            try
            {
                File.AppendAllLines(App.logFilePath, new[] { logMessage });
                LogSystemEvent($"Money-B-Mine: {logMessage}"); // Save system event
            }
            finally
            {
                MessageBox.Show(displayMessage, "Money-B-Mine", MessageBoxButton.OK, MessageBoxImage.Information);
                if (closeApplication == true)
                {
                    Application.Current.Shutdown(); // exit the application
                }
            }
        }

        /// <summary>
        /// Logs a system even
        /// </summary>
        internal static void LogSystemEvent(string message)
        {
            EventLog.WriteEntry(".NET Runtime", message, EventLogEntryType.Information, 1000);
        }

        /// <summary>
        /// Logs unhandled exception to NLog & file system (text)
        /// </summary>
        internal static void LogException(Exception exception, string source, bool closeApplication = true)
        {
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();

                string message = $"\n{string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version)}\n{exception}";
                logger.Error(exception, message);
                LogEvent(message, closeApplication);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in LogUnhandledException");
            }
        }
    }
}
