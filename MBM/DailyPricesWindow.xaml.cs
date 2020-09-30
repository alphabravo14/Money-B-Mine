using MBM.Classes;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace MBM
{
    /// <summary>
    /// Interaction logic for DailyPricesWindow.xaml
    /// </summary>
    public partial class DailyPricesWindow : Window
    {
        private bool isEditing = false;
        private int recordID = 0;

        public DailyPricesWindow(int idDailyPrice = 0)
        {
            InitializeComponent();
            if (idDailyPrice != 0) LoadRecord(idDailyPrice); // Load record for editing if ID was passed
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Close window
        }

        #region Load Record For Editing

        /// <summary>
        /// Loads the passed record for editing
        /// </summary>
        private void LoadRecord(int id)
        {
            isEditing = true;
            recordID = id;

            using (SqlConnection sqlConnection = new SqlConnection(App.connectionString))
            {
                string commandText = $"SELECT * FROM DailyPrices WHERE ID = {id}";

                SqlCommand sqlCommand = new SqlCommand(commandText, sqlConnection);
                sqlConnection.Open();
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();

                while (sqlDataReader.Read()) // Opens the record and loads in the form fields
                {
                    datePicker.SelectedDate = DateTime.Parse(sqlDataReader["date"].ToString());
                    textboxExchange.Text = (sqlDataReader["exchange"].ToString());
                    textboxSymbol.Text = (sqlDataReader["stock_symbol"].ToString());
                    textboxPriceOpen.Text = (sqlDataReader["stock_price_open"].ToString());
                    textboxPriceClose.Text = (sqlDataReader["stock_price_close"].ToString());
                    textboxPriceLow.Text = (sqlDataReader["stock_price_low"].ToString());
                    textboxPriceHigh.Text = (sqlDataReader["stock_price_high"].ToString());
                    textboxPriceAdjClose.Text = (sqlDataReader["stock_price_adj_close"].ToString());
                    textboxVolume.Text = (sqlDataReader["stock_volume"].ToString());
                }
                sqlDataReader.Close();
                sqlConnection.Close();
            }
        }

        #endregion

        #region Add / Update Record

        /// <summary>
        /// Performs validation using FluentValidation and will then either update or add a new record depending on what mode the window is in
        /// </summary>
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            ProcessRecord();
        }

        private void ProcessRecord()
        {
            // Create new class object for validation and record manipulation
            DailyPrices dailyPrices = new DailyPrices();
            dailyPrices.Date = datePicker.SelectedDate.ToString();
            dailyPrices.Exchange = textboxExchange.Text;
            dailyPrices.Symbol = textboxSymbol.Text;
            dailyPrices.PriceOpen = textboxPriceOpen.Text;
            dailyPrices.PriceClose = textboxPriceClose.Text;
            dailyPrices.PriceLow = textboxPriceLow.Text;
            dailyPrices.PriceHigh = textboxPriceHigh.Text;
            dailyPrices.StockPriceAdj = textboxPriceAdjClose.Text;
            dailyPrices.Volume = textboxVolume.Text;

            if (!Validation.ValidateRecord(dailyPrices))
            {
                return; // Exit the method if validation fails
            }
            else
            {
                if (isEditing) // Update current record
                {
                    using (SqlConnection sqlConnection = new SqlConnection(App.connectionString))
                    {
                        string commandText = "UPDATE DailyPrices SET " +
                                             "date = @date, " +
                                             "exchange = @exchange, " +
                                             "stock_symbol = @stock_symbol, " +
                                             "stock_price_open = @stock_price_open, " +
                                             "stock_price_close = @stock_price_close, " +
                                             "stock_price_low = @stock_price_low, " +
                                             "stock_price_high = @stock_price_high, " +
                                             "stock_price_adj_close = @stock_price_adj_close, " +
                                             "stock_volume = @stock_volume " +
                                             "WHERE ID = @ID";

                        //"UPDATE DailyPrices SET date = @date, exchange = @exchange, stock_symbol = @stock_symbol, stock_price_open = @ stock_price_open, stock_price_close = @stock_price_close, stock_price_low = @stock_price_low, stock_price_high = @stock_price_high, stock_price_adj_close = @stock_price_adj_close, stock_volume = @stock_volume WHERE ID = @ID)"

                        SqlCommand sqlCommand = new SqlCommand(commandText, sqlConnection);
                        sqlCommand.Parameters.Add("@date", SqlDbType.Date).Value = dailyPrices.Date;
                        sqlCommand.Parameters.Add("@exchange", SqlDbType.VarChar, 20).Value = dailyPrices.Exchange;
                        sqlCommand.Parameters.Add("@stock_symbol", SqlDbType.VarChar, 20).Value = dailyPrices.Symbol;
                        sqlCommand.Parameters.Add("@stock_price_open", SqlDbType.Float).Value = dailyPrices.PriceOpen;
                        sqlCommand.Parameters.Add("@stock_price_close", SqlDbType.Float).Value = dailyPrices.PriceClose;
                        sqlCommand.Parameters.Add("@stock_price_low", SqlDbType.Float).Value = dailyPrices.PriceLow;
                        sqlCommand.Parameters.Add("@stock_price_high", SqlDbType.Float).Value = dailyPrices.PriceHigh;
                        sqlCommand.Parameters.Add("@stock_price_adj_close", SqlDbType.Float).Value = dailyPrices.StockPriceAdj;
                        sqlCommand.Parameters.Add("@stock_volume", SqlDbType.Int).Value = dailyPrices.Volume;
                        sqlCommand.Parameters.Add("@ID", SqlDbType.Int).Value = recordID;

                        sqlConnection.Open();
                        sqlCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                }
                else // Add new record
                {
                    using (SqlConnection sqlConnection = new SqlConnection(App.connectionString))
                    {
                        string commandText = "INSERT INTO DailyPrices " +
                                             "(date, exchange, stock_symbol, stock_price_open, stock_price_close, " +
                                             "stock_price_low, stock_price_high, stock_price_adj_close, stock_volume) " +
                                             "VALUES (@date, @exchange, @stock_symbol, @stock_price_open, @stock_price_close, " +
                                             "@stock_price_low, @stock_price_high, @stock_price_adj_close, @stock_volume)";
                        SqlCommand sqlCommand = new SqlCommand(commandText, sqlConnection);
                        sqlCommand.Parameters.Add("@date", SqlDbType.Date).Value = dailyPrices.Date;
                        sqlCommand.Parameters.Add("@exchange", SqlDbType.VarChar, 20).Value = dailyPrices.Exchange;
                        sqlCommand.Parameters.Add("@stock_symbol", SqlDbType.VarChar, 20).Value = dailyPrices.Symbol;
                        sqlCommand.Parameters.Add("@stock_price_open", SqlDbType.Float).Value = dailyPrices.PriceOpen;
                        sqlCommand.Parameters.Add("@stock_price_close", SqlDbType.Float).Value = dailyPrices.PriceClose;
                        sqlCommand.Parameters.Add("@stock_price_low", SqlDbType.Float).Value = dailyPrices.PriceLow;
                        sqlCommand.Parameters.Add("@stock_price_high", SqlDbType.Float).Value = dailyPrices.PriceHigh;
                        sqlCommand.Parameters.Add("@stock_price_adj_close", SqlDbType.Float).Value = dailyPrices.StockPriceAdj;
                        sqlCommand.Parameters.Add("@stock_volume", SqlDbType.Int).Value = dailyPrices.Volume;

                        sqlConnection.Open();
                        sqlCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                }
                Close(); // Close window
            }
        }

        /// <summary>
        /// Also submit form when enter key is pressed in addition to button clicks
        /// </summary>
        private void KeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ButtonSave_Click(sender, e);
            }
        }

        #endregion

    }
}
