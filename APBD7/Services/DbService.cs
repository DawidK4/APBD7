using System.Data;
using System.Data.SqlClient;

namespace APBD7.Services;

public class DbService
{
    private readonly string _connectionString = @"Data Source=DAWID\SQLEXPRESS;Initial Catalog=master;Integrated Security=True";

    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(_connectionString);
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<(int rowNumber, string errorMessage)> CheckDataValidity(int IdProduct, int IdWarehouse, int Amount)
    {
        try
        {
            await using var connection = await GetConnection();
        
            string checkProduct = @"SELECT IdProduct FROM Product WHERE IdProduct = @IdProduct";
            string checkWarehouse = @"SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        
            var productCommand = new SqlCommand(checkProduct, connection);
            productCommand.Parameters.AddWithValue("@IdProduct", IdProduct);
            var productReader = await productCommand.ExecuteReaderAsync();
            bool productExists = productReader.HasRows;
            decimal Price = productReader.GetInt32(3);
            await productReader.CloseAsync();
        
            var warehouseCommand = new SqlCommand(checkWarehouse, connection);
            warehouseCommand.Parameters.AddWithValue("@IdWarehouse", IdWarehouse);
            var warehouseReader = await warehouseCommand.ExecuteReaderAsync();
            bool warehouseExists = warehouseReader.HasRows;
            await warehouseReader.CloseAsync();
            
            if (!productExists || !warehouseExists || Amount < 0)
            {
                return (-1, "Provided data is invalid!");
            }

            string checkOrder = @"select IdOrder from [Order] where IdProduct = @IdProduct and Amount = @Amount";
            var orderCommand = new SqlCommand(checkOrder, connection);
            orderCommand.Parameters.AddWithValue("@IdProduct", IdProduct);
            orderCommand.Parameters.AddWithValue("@Amount", Amount);
            var orderReader = await orderCommand.ExecuteReaderAsync();
            bool orderExists = await orderReader.ReadAsync();

            if (!orderExists)
            {
                    return (-1, "Order with provided data does not exist!");
            }

            int IdOrder = orderReader.GetInt32(0);

            if (await orderReader.ReadAsync()) 
            {
                if (!orderReader.IsDBNull(4))
                {
                    DateTime FulfilledAt = orderReader.GetDateTime(3);
                    DateTime CreatedAt = orderReader.GetDateTime(4);

                    if (CreatedAt > FulfilledAt)
                    {
                        return (-1, "Created at date is greater that fulfilled at date!");
                    }
                }
            }

            orderReader.CloseAsync();

            string checkIfOrderIsComplete = @"select IdOrder from Product_Warehouse where IdOrder = @IdOrder";
            var checkIfOrderIsCompleteCommand = new SqlCommand(checkIfOrderIsComplete, connection);
            checkIfOrderIsCompleteCommand.Parameters.AddWithValue("@IdOrder", IdOrder);
            var productWarehouseReader = await checkIfOrderIsCompleteCommand.ExecuteReaderAsync();
            bool orderIsComplete = productWarehouseReader.HasRows;

            if (orderIsComplete)
            {
                return (-1, "Order has already been completed!");
            }

            productWarehouseReader.CloseAsync();

            DateTime currentDate = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                string updateOrderData = @"update [Order] set FulfilledAt = @currentDate where IdOrder = @IdOrder";
                var updateOrderCommand = new SqlCommand(updateOrderData, connection);
                updateOrderCommand.Parameters.AddWithValue("@currentDate", currentDate);
                updateOrderCommand.Parameters.AddWithValue("@IdOrder", IdOrder);

                decimal orderPrice = Price * Amount;
                string insertQuery =
                    @"INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, Price, CreatedAt) OUTPUT INSERTED.IdProduct_Warehouse VALUES (@IdProduct, @IdWarehouse, @Price, @CreatedAt)";
                var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@IdProduct", IdProduct);
                insertCommand.Parameters.AddWithValue("@IdWarehouse", IdWarehouse);
                insertCommand.Parameters.AddWithValue("@Price", orderPrice);
                insertCommand.Parameters.AddWithValue("@CreatedAt", currentDate);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (-1, "An error occured during insertion!");
            }
            finally
            {
                await connection.CloseAsync();
            }
            
            return (1, "Alles in Ordnung");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}