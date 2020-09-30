using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.Services;

namespace WebService
{
    /// <summary>
    /// This web service will allow users to return all daily price records from the AWS database as a data table
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    public class WebService : System.Web.Services.WebService
    {
        [WebMethod]
        public DataTable LoadData(int numberOfRecords)
        {
            // Grab daily prices from database stored procedure and return data table
            string connectionString = ConfigurationManager.ConnectionStrings["AmazonConnectionString"].ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
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
    }
}
