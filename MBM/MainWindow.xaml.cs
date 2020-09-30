using MBM.Classes;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MBM
{
    public partial class MainWindow : Window
    {
        private DataTable dataTableDailyPrices;
        private bool canManipulateData = false;
        private bool isLoadingData = false;
        private int numberOfRecords = 100;
        private string xmlFilePath = App.xmlFilePathApp;

        public MainWindow(bool canManipulateData = false)
        {
            this.canManipulateData = canManipulateData;
            InitializeComponent();
            InitializeData(canManipulateData);
        }

        #region Data Loading

        /// <summary>
        /// Check database connections and load data in the background on a separate thread to avoid UI freezing
        /// </summary>
        private void InitializeData(bool canManipulateData)
        {
            if (DockLoading != null) DockLoading.Visibility = Visibility.Visible;
            numberOfRecords = NumberOfRecords();
            if (File.Exists(App.xmlFilePathMyDocs)) this.xmlFilePath = App.xmlFilePathMyDocs; // Use my documents XML file if it's been created

            var queue = new Helper.BackgroundQueue();
            queue.QueueTask(() => { LoadData(); });

            if (canManipulateData == true) // If user is an analyst then allow data manipulation and viewing machine specs
            {
                if (buttonAdd != null) buttonAdd.Visibility = Visibility.Visible;
                if (buttonEdit != null) buttonEdit.Visibility = Visibility.Visible;
                if (buttonDelete != null) buttonDelete.Visibility = Visibility.Visible;
                if (buttonSpecs != null) buttonSpecs.Visibility = Visibility.Visible;
            }
            else // Otherwise hide
            {
                if (buttonAdd != null) buttonAdd.Visibility = Visibility.Hidden;
                if (buttonEdit != null) buttonEdit.Visibility = Visibility.Hidden;
                if (buttonDelete != null) buttonDelete.Visibility = Visibility.Hidden;
                if (buttonSpecs != null) buttonSpecs.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Loads the daily prices from the database and updates the local copy, or uses from the local XML copy if connection is unavailable
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (isLoadingData) return; // Exit if already loading data
                else isLoadingData = true;

                if (this.canManipulateData == true) // If analyst then load directly from DB stored procedure
                {
                    dataTableDailyPrices = LoadDailyPrices();
                }
                else // Otherwise use the web service
                {
                    var webService = new net.azurewebsites.moneybminewebservice.WebService();
                    dataTableDailyPrices = webService.LoadData(numberOfRecords);
                }

                Dispatcher.Invoke(DispatcherPriority.Background, // Update the data grid
                           new Action(() => DataGridDailyPrices.ItemsSource = dataTableDailyPrices.DefaultView));
                
                dataTableDailyPrices.WriteXml(App.xmlFilePathMyDocs); // Save local XML copy for future use if needed
                MessageBox.Show(App.xmlFilePathMyDocs);

                Logging.LogSystemEvent("Money-B-Mine Loaded. Connected to remote DB, local XML file created."); // Log system event
            }
            catch (SqlException)
            {
                Dispatcher.Invoke(DispatcherPriority.Background,
                           new Action(() => LoadData(xmlFilePath))); // Use local XML if database connection issues

                string logMessage = "Remote database unavailable - using local version instead. Record manipulation will be unavailable.";
                Logging.LogEvent(logMessage, logMessage, closeApplication: false);
            }
            catch (System.Net.WebException)
            {
                Dispatcher.Invoke(DispatcherPriority.Background,
                           new Action(() => LoadData(xmlFilePath))); // Use local XML if web service connection issues

                string logMessage = "Remote web service unavailable - using local version instead. Record manipulation will be unavailable.";
                Logging.LogEvent(logMessage, logMessage, closeApplication: false);
            }
            finally
            {
                Dispatcher.Invoke(DispatcherPriority.Background,
                           new Action(() => DockLoading.Visibility = Visibility.Hidden)); // Hide loading message
                isLoadingData = false;
            }
        }

        /// <summary>
        /// Override - Uses the locally stored XML file to load data for viewing in the event of the DB connection being unavailable
        /// </summary>
        private void LoadData(string filePath)
        {
            try
            {
                DataSet dataSet = new DataSet();
                dataSet.ReadXml(filePath);
                DataView dataView = new DataView();

                if (numberOfRecords == 0)
                {
                    dataView = new DataView(dataSet.Tables[0]); // Get all records
                }
                else
                {
                    var xmlRecords = dataSet.Tables[0].AsEnumerable().Take(numberOfRecords).CopyToDataTable(); // Select x amount based on drop down lis
                    dataView = new DataView(xmlRecords);
                }

                DataGridDailyPrices.ItemsSource = dataView;
                Logging.LogSystemEvent("Money-B-Mine XML Loaded. Remote DB unavailable, using local."); // Log system event

                // Hide data manipulation buttons
                buttonAdd.Visibility = Visibility.Hidden;
                buttonEdit.Visibility = Visibility.Hidden;
                buttonDelete.Visibility = Visibility.Hidden;
            }
            catch (Exception)
            {
                string logMessage = "An error occured trying to load the local data. The application will now close";
                Logging.LogEvent(logMessage, logMessage, closeApplication: true);
            }
        }

        /// <summary>
        /// Loads data from AWS stored procedure
        /// </summary>
        private DataTable LoadDailyPrices()
        {
            using (SqlConnection sqlConnection = new SqlConnection(App.connectionString))
            {
                SqlCommand sqlCommand = new SqlCommand("stpGetDailyPrices", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@NumberOfRecords", numberOfRecords);
                sqlCommand.CommandTimeout = 0; // no timeout
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                DataTable dataTable = new DataTable("DailyPrices");

                sqlDataAdapter.Fill(dataTable);
                return dataTable;
            }
        }

        private void DailyPricesWindow_Closed(object sender, EventArgs e)
        {
            InitializeData(this.canManipulateData); // Reload data when data window is closed
        }

        #endregion

        #region Data Grid Filtering

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDataGrid(); // Filter when date is changed
        }

        private void TextboxFilter_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterDataGrid(); // Filter when text is entered
        }

        /// <summary>
        /// Builds the query and filters the daily prices datagrid based on user input and selected date etc
        /// </summary>
        private void FilterDataGrid()
        {
            StringBuilder filterString = new StringBuilder();

            // Build filter query
            if (!string.IsNullOrEmpty(dateFilter.SelectedDate.ToString()))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "date", dateFilter.SelectedDate.ToString().Trim()));
            }

            if (!string.IsNullOrEmpty(textboxExchange.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "exchange", textboxExchange.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxSymbol.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_symbol", textboxSymbol.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxPriceOpen.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_price_open", textboxPriceOpen.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxPriceClose.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_price_close", textboxPriceClose.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxPriceLow.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_price_low", textboxPriceLow.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxPriceHigh.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_price_high", textboxPriceHigh.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxPriceAdjClose.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_price_adj_close", textboxPriceAdjClose.Text.Trim()));
            }

            if (!string.IsNullOrEmpty(textboxVolume.Text))
            {
                filterString.Append(string.Format("CONVERT({0}, System.String) LIKE '%{1}%' AND ", "stock_volume", textboxVolume.Text.Trim()));
            }

            // Remove last 'AND' from query if required
            if (filterString.ToString().Trim().EndsWith("AND"))
            {
                dataTableDailyPrices.DefaultView.RowFilter = filterString.ToString().Remove(filterString.ToString().LastIndexOf("AND"));
            }
            else
            {
                dataTableDailyPrices.DefaultView.RowFilter = filterString.ToString();
            }

            DataGridDailyPrices.ItemsSource = dataTableDailyPrices.DefaultView;
        }

        /// <summary>
        /// Returns query int for number of records to be returned
        /// </summary>
        /// <returns></returns>
        private int NumberOfRecords()
        {
            string comboRecordsText = comboRecords.Text.ToString();

            if (comboRecordsText == "") return 100; // Returns 100 for initial load

            if (comboRecordsText == "100 Records")
            {
                return 100;
            }
            else if (comboRecordsText == "500 Records")
            {
                return 500;
            }
            else if (comboRecordsText == "1000 Records")
            {
                return 1000;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Data Record Manipulation

        /// <summary>
        /// Opens the daily prices record window for adding a new record
        /// </summary>
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            DailyPricesWindow dailyPricesWindow = new DailyPricesWindow();
            dailyPricesWindow.Closed += DailyPricesWindow_Closed;
            dailyPricesWindow.ShowDialog();
        }

        /// <summary>
        /// Opens the daily prices record window for editing
        /// </summary>
        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridDailyPrices.SelectedItem != null)
            {
                DataRowView datagridRow = (DataRowView)DataGridDailyPrices.SelectedItems[0];
                DailyPricesWindow dailyPricesWindow = new DailyPricesWindow(Int32.Parse(datagridRow["ID"].ToString())); // Pass the ID for record editing
                dailyPricesWindow.Closed += DailyPricesWindow_Closed;
                dailyPricesWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Deletes the selected record from the database and grid
        /// </summary>
        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRecord();
        }

        private void DeleteSelectedRecord()
        {
            if (DataGridDailyPrices.SelectedItem == null) return; // Exit if no item chosen for deletion

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult != MessageBoxResult.Yes) return; // Exit if deletion is not confirmed

            DataRowView datagridRow = (DataRowView)DataGridDailyPrices.SelectedItems[0];
            int recordID = Int32.Parse(datagridRow["ID"].ToString());

            // Remove record from database
            using (SqlConnection sqlConnection = new SqlConnection(App.connectionString))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand($"DELETE FROM DailyPrices WHERE ID = {recordID}", sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
                sqlConnection.Close();
            }

            // Remove from data table & grid
            DataRow[] drr = dataTableDailyPrices.Select($"ID = {recordID}");
            for (int i = 0; i < drr.Length; i++)
            {
                drr[i].Delete();
            }

            dataTableDailyPrices.AcceptChanges();
        }

        private void comboRecords_SelectionChanged(object sender, EventArgs e)
        {
            InitializeData(this.canManipulateData); // Reload data based on number of records selected
        }

        #endregion

        #region CSV Report Creation

        private void btnSaveCSV_Click(object sender, RoutedEventArgs e)
        {
            ExportCSV();
        }

        /// <summary>
        /// Allows the user to choose the location for the CSV to be exported
        /// </summary>
        private void ExportCSV()
        {
            if (DataGridDailyPrices.Items.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV (*.csv)|*.csv";
                saveFileDialog.FileName = "Report.csv";
                bool fileError = false;

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (File.Exists(saveFileDialog.FileName)) // Remove existing file if needing to overwrite
                    {
                        try
                        {
                            File.Delete(saveFileDialog.FileName);
                        }
                        catch (IOException)
                        {
                            fileError = true;
                            string logMessage = "It wasn't possible to write the data to the disk.";
                            Logging.LogEvent(logMessage, logMessage, closeApplication: false);
                        }
                    }
                    if (!fileError) // If no errors have occured then export the new CSV
                    {
                        try
                        {
                            DataTable dt = new DataTable();
                            dt = ((DataView)DataGridDailyPrices.ItemsSource).ToTable();
                            string result = WriteDataTable(dt);
                            File.AppendAllText(saveFileDialog.FileName, result, UnicodeEncoding.UTF8);

                            string logMessage = "Data Exported Successfully";
                            Logging.LogEvent(logMessage, logMessage, closeApplication: false);
                        }
                        catch (Exception ex)
                        {
                            Logging.LogEvent("Error :" + ex.Message);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("No Records To Export", "Info");
            }
        }

        /// <summary>
        /// Helper for CSV export
        /// </summary>
        private string WriteDataTable(DataTable dataTable)
        {
            string output = "";

            // Get the last column
            string lastColumnName = dataTable.Columns[dataTable.Columns.Count - 1].ColumnName;

            // Get headers
            foreach (DataColumn column in dataTable.Columns)
            {
                if (lastColumnName != column.ColumnName)
                {
                    output += (column.ColumnName.ToString() + ",");
                }
                else
                {
                    output += (column.ColumnName.ToString() + "\n");
                }
            }
            // Get the actual data
            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (lastColumnName != column.ColumnName)
                    {
                        output += (row[column].ToString() + ",");
                    }
                    else
                    {
                        output += (row[column].ToString() + "\n");
                    }
                }
            }
            return output;
        }

        #endregion

        #region Show System Specs

        /// <summary>
        /// Show system specifications
        /// </summary>
        private void btnShowSpecs_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(SystemSpecs.SystemDetails(), "System Specifications", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Help

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help helpWindow = new Help();
            helpWindow.ShowDialog();
        }

        #endregion

    }
}
