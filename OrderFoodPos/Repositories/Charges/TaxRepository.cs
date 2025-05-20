using Dapper;
using OrderFoodPos.Models.Charges;
using System.Data;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Charges
{
    public class TaxRepository : BaseRepository<TaxModel>
    {
        public TaxRepository(IDbConnection db) : base(db) { }

        /// <summary>
        /// 取得指定商店的稅務設定
        /// </summary>
        public async Task<TaxModel?> GetByStoreIdAsync(int storeId)
        {
            var sql = @"SELECT StoreId, TaxRate, TaxIncluded, RoundingType, TaxFreeThreshold
						FROM StoreTaxSettings
						WHERE StoreId = @StoreId";

            return await GetAsync(sql, new { StoreId = storeId });
        }

        /// <summary>
        /// 新增或更新稅務設定
        /// </summary>

        public async Task InsertTaxSettingAsync(TaxModel model)
        {
            var sql = @"
INSERT INTO StoreTaxSettings (StoreId, TaxRate, TaxIncluded, RoundingType, TaxFreeThreshold)
VALUES (@StoreId, @TaxRate, @TaxIncluded, @RoundingType, @TaxFreeThreshold)";
            await _dbConnection.ExecuteAsync(sql, model);
        }

        public async Task UpdateTaxSettingAsync(TaxModel model)
        {
            var sql = @"
UPDATE StoreTaxSettings
SET TaxRate = @TaxRate,
	TaxIncluded = @TaxIncluded,
	RoundingType = @RoundingType,
	TaxFreeThreshold = @TaxFreeThreshold,
UpdatedAt = GETDATE()
WHERE StoreId = @StoreId";
            await _dbConnection.ExecuteAsync(sql, model);
        }




    }
}
