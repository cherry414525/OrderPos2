using System.Data;
using System.Threading.Tasks;
using Dapper;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Repositories.Payments
{
    public class StoreEcpayRepository
    {
        private readonly IDbConnection _dbConnection;

        public StoreEcpayRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<StoreEcpay?> GetStoreEcpayByStoreIdAsync(string storeId)
        {
            var sql = "SELECT * FROM StoreEcpay WHERE StoreID = @StoreID";
            return await _dbConnection.QueryFirstOrDefaultAsync<StoreEcpay>(sql, new { StoreID = storeId });
        }
    }
}
