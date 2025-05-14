using Dapper;
using OrderFoodPos.Models.Menu;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Menu
{
	public class ItemRepository : BaseRepository<MenuItem>
	{
		public ItemRepository(IDbConnection db) : base(db) { }

		/// <summary>
		/// 取得指定商店的單一餐點資料
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<MenuItem?> GetByIdAsync(int id)
		{
			var sql = "SELECT * FROM MenuItems WHERE Id = @Id";
			return await GetAsync(sql, new { Id = id }); // 使用 BaseRepository 的 GetAsync
		}


		public async Task<IEnumerable<MenuItem>> GetAllByStoreIdAsync(int StoreId)
		{
			var sql = "SELECT * FROM MenuItems WHERE StoreId = @StoreId ORDER BY Sequence";
			return await GetAllAsync(sql, new { StoreId = StoreId }); // 使用 BaseRepository 的通用查詢
		}



		/// <summary>
		/// 新增餐點資料並回傳新 ID
		/// </summary>
		public async Task<int> AddMenuItemAsync(MenuItem model)
		{
			var sql = @"
			INSERT INTO MenuItems 
				(StoreId, CategoryId, Name, Description, Price, ImageUrl, Sequence, Meter, Stock, CreatedDate)
			VALUES 
				(@StoreId, @CategoryId, @Name, @Description, @Price, @ImageUrl, @Sequence, @Meter, @Stock, GETDATE());
			SELECT CAST(SCOPE_IDENTITY() AS INT);";

			return await ExecuteScalarAsync<int>(sql, model); // 呼叫共用方法寫入
		}


		//根據商店Id刪除商品
		public async Task DeleteAllMenuItemsByStoreIdAsync(int storeId)
		{
			string sql = "DELETE FROM MenuItems WHERE StoreId = @StoreId";
			await ExecuteAsync(sql, new { StoreId = storeId });
		}

		public async Task DeleteAllItemsByStoreIdAsync(int storeId)
		{
			string sql = "DELETE FROM MenuItems WHERE StoreId = @StoreId";
			await _dbConnection.ExecuteAsync(sql, new { StoreId = storeId });
		}

		public async Task DeleteAllCategoriesByStoreIdAsync(int storeId)
		{
			string sql = "DELETE FROM MenuCategories WHERE StoreId = @StoreId";
			await _dbConnection.ExecuteAsync(sql, new { StoreId = storeId });
		}

		public async Task<int> CreateCategoryAsync(MenuCategory category)
		{
			var sql = @"INSERT INTO MenuCategories (Name, StoreId, Sequence, CreatedDate)
                VALUES (@Name, @StoreId, @Sequence, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

			return await ExecuteScalarAsync<int>(sql, category); // 回傳新 ID
		}


		public async Task DeleteMenuItemAsync(int id)
		{
			var sql = "DELETE FROM MenuItems WHERE Id = @Id"; // 確保表名正確
			await _dbConnection.ExecuteAsync(sql, new { Id = id });
		}



	}
}
