using Dapper;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Models.Store;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Store
{
	public class StoresRepository : BaseRepository<StoreModel>
	{
		public StoresRepository(IDbConnection db) : base(db) { }


		//獲取商店資料
		public async Task<StoreDetailModel?> GetStoreByIdAsync(int storeId)
		{
			string sql = "SELECT * FROM Stores WHERE Id = @Id";
			return await _dbConnection.QueryFirstOrDefaultAsync<StoreDetailModel>(sql, new { Id = storeId });
		}



		//更新商家資料
		public async Task<bool> UpdateStoreAsync(StoreUpdateRequest req)
		{
			string sql = @"
UPDATE Stores SET
	StoreName = @StoreName,
	Address = @Address,
	PhoneNumber = @PhoneNumber,
	Description = @Description,
	Opened = @Opened,
	BusinessType = @BusinessType,
	Image = @Image,
	UpdatedDate = @UpdatedDate,
	Facebook = @Facebook,
	Instagram = @Instagram,
	Line = @Line
WHERE Id = @Id";

			return await _dbConnection.ExecuteAsync(sql, req) > 0;
		}
	}
}
