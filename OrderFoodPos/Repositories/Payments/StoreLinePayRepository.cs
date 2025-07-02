using System.Data;
using System.Threading.Tasks;
using Dapper;
using OrderFoodPos.Models;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Repositories.Payments
{
    public class StoreLinePayRepository
    {
        private readonly IDbConnection _dbConnection;

        public StoreLinePayRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<StoreLinePay?> GetStoreLinePayByStoreIdAsync(string storeId)
        {
            var sql = "SELECT * FROM StoreLinePay WHERE StoreID = @StoreID";
            return await _dbConnection.QueryFirstOrDefaultAsync<StoreLinePay>(sql, new { StoreID = storeId });
        }
    }
}

