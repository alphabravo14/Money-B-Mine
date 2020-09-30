using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MBM
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            passwordBox.Focus();
        }

        #region Login

        /// <summary>
        /// Processes simple login - this can be extended to use a database user system when scaling to a production environment
        /// </summary>
        private void buttonLogin_Click(object sender, RoutedEventArgs e)
        {
            if (comboUserType.Text.ToString() == "Analyst" && passwordBox.Password == "Password1")
            {
                MainWindow mainWindow = new MainWindow(canManipulateData: true); // Load window for analysts
                mainWindow.ShowDialog();
                Close();
            }
            else if (comboUserType.Text.ToString() == "General User" && passwordBox.Password == "Password2")
            {
                MainWindow mainWindow = new MainWindow(canManipulateData: false); // Load window for general users
                mainWindow.ShowDialog();
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect Login Details", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void comboUserType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (passwordBox != null) passwordBox.Focus(); // Re-focus on password when drop down changes
        }

        private void KeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                buttonLogin_Click(sender, e);
            }
        }

        #endregion

    }
}
