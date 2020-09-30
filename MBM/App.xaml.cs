using MBM.Classes;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MBM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Set local paths
        //public static readonly string connectionString = ConfigurationManager.ConnectionStrings["LocalDatabaseConnectionString"].ConnectionString;
        public static readonly string connectionString = ConfigurationManager.ConnectionStrings["AmazonConnectionString"].ConnectionString;
        //public static readonly string rootFolderPath = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.FullName.ToString(); // Development
        public static readonly string rootFolderPath = new DirectoryInfo(Environment.CurrentDirectory).FullName.ToString(); // Release
        public static readonly string xmlFilePath = $@"{rootFolderPath}\App_Data\DailyPrices.xml";
        public static readonly string logFilePath = $@"{rootFolderPath}\Logs\{DateTime.Now:yyyy-dd-M-HH-mm-ss}.txt";

        protected override void OnStartup(StartupEventArgs e)
        {
            // Custom event handlers to automatically select all text when a textbox recieves focus
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));

            SetupExceptionHandling(); // Initiate global error handling
            base.OnStartup(e);
        }

        #region TextBox Select All Text

        /// <summary>
        /// Find the TextBox and if the text box is not yet focused, give it the focus and stop further processing of this click event.
        /// </summary>
        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            DependencyObject parent = e.OriginalSource as UIElement;

            while (parent != null && !(parent is TextBox))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }
        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        #endregion

        #region Global Exception Handling

        /// <summary>
        /// Use NLog to catch exceptions thrown from all threads in the AppDomain, from the UI dispatcher thread and async functions
        /// </summary>
        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Logging.LogException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", closeApplication: true);

            DispatcherUnhandledException += (s, e) =>
            {
                Logging.LogException(e.Exception, "Application.Current.DispatcherUnhandledException", closeApplication: true);
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logging.LogException(e.Exception, "TaskScheduler.UnobservedTaskException", closeApplication: true);
                e.SetObserved();
            };
        }

        #endregion

    }
}
