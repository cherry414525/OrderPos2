using Dapper;
using OrderFoodPos.Models.Charges;
using System.Data;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Charges
{
    public class ServiceFeeRepository : BaseRepository<ServiceFeeModel>
    {
        public ServiceFeeRepository(IDbConnection db) : base(db) { }

        /// <summary>
        /// 取得指定商店的服務費設定
        /// </summary>
        public async Task<ServiceFeeModel?> GetByStoreIdAsync(int storeId)
        {
            var sql = @"SELECT StoreId, ServiceFeeRate, FeeIncluded
                        FROM StoreServiceFee
                        WHERE StoreId = @StoreId";

            return await GetAsync(sql, new { StoreId = storeId });
        }

        /// <summary>
        /// 新增服務費設定
        /// </summary>
        public async Task InsertServiceFeeSettingAsync(ServiceFeeModel model)
        {
            var sql = @"
INSERT INTO StoreServiceFee (StoreId, ServiceFeeRate, FeeIncluded)
VALUES (@StoreId, @ServiceFeeRate, @FeeIncluded)";
            await _dbConnection.ExecuteAsync(sql, model);
        }

        /// <summary>
        /// 更新服務費設定
        /// </summary>
        public async Task UpdateServiceFeeSettingAsync(ServiceFeeModel model)
        {
            var sql = @"
UPDATE StoreServiceFee
SET ServiceFeeRate = @ServiceFeeRate,
    FeeIncluded = @FeeIncluded,
    UpdateDate = GETDATE()
WHERE StoreId = @StoreId";
            await _dbConnection.ExecuteAsync(sql, model);
        }
    }
}
