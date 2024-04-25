using System.Data;
using System.Data.SqlClient;

namespace APBD7.Services;

public class DbService (IConfiguration configuration)
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString(@"Data Source=DAWID\SQLEXPRESS;Initial Catalog=master;Integrated Security=True"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }
}