using System;
using Dapper;
using System.Data;
using System.Threading.Tasks;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Repositories.Payments
{
    public class StoreJkoPayRepository
    {
        private readonly IDbConnection _dbConnection;

        public StoreJkoPayRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<StoreJkoPay?> GetStoreJkoPayByStoreIdAsync(string storeId)
        {
            var sql = "SELECT * FROM StoreJkoPay WHERE StoreID = @StoreID";
            return await _dbConnection.QueryFirstOrDefaultAsync<StoreJkoPay>(sql, new { StoreID = storeId });
        }
    }
}
