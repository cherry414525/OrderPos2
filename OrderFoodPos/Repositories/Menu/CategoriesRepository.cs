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
	public class CategoriesRepository : BaseRepository<MenuCategory>
	{
		public CategoriesRepository(IDbConnection dbConnection) : base(dbConnection)
		{
		}

		/// <summary>
		/// 取得指定商店的所有分類
		/// </summary>
		public async Task<IEnumerable<MenuCategory>> GetAllByStoreIdAsync(int storeId)
		{
			var sql = @"SELECT * FROM MenuCategories WHERE StoreId = @StoreId ORDER BY Sequence";
			return await GetAllAsync(sql, new { StoreId = storeId }); // 共用 GetAllAsync 方法
		}

		/// <summary>
		/// 新增分類並回傳新 ID
		/// </summary>
		public async Task<int> AddCategoryAsync(MenuCategory category)
		{
			var sql = @"
                INSERT INTO MenuCategories (StoreId, Name, CreatedDate, UpdatedDate, Sequence)
                VALUES (@StoreId, @Name, @CreatedDate, @UpdatedDate, @Sequence);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
			return await ExecuteScalarAsync<int>(sql, category); // 使用 Base 的 ExecuteScalarAsync
		}

		/// <summary>
		/// 更新分類名稱、排序、商店ID（必要時）
		/// </summary>
		public async Task<int> UpdateCategoryAsync(int id, string name, int storeId, int sequence)
		{
			var sql = @"
		UPDATE MenuCategories 
		SET Name = @Name, StoreId = @StoreId, Sequence = @Sequence, UpdatedDate = GETDATE()
		WHERE Id = @Id";

			return await UpdateAsync(sql, new
			{
				Id = id,
				Name = name,
				StoreId = storeId,
				Sequence = sequence
			});
		}


		/// <summary>
		/// 刪除分類
		/// </summary>
		public async Task<int> DeleteCategoryAsync(int id)
		{
			var sql = @"DELETE FROM MenuCategories WHERE Id = @Id";
			return await DeleteAsync(sql, new { Id = id });
		}

		public async Task<IEnumerable<MenuCategory>> GetCategoriesByStoreIdAsync(int storeId)
		{
			var sql = "SELECT * FROM MenuCategories WHERE StoreId = @StoreId ORDER BY Sequence";
			return await _dbConnection.QueryAsync<MenuCategory>(sql, new { StoreId = storeId });

		}

		//根據商店ID刪除全部類別
		public async Task DeleteAllCategoriesByStoreIdAsync(int storeId)
		{
			string sql = "DELETE FROM MenuCategories WHERE StoreId = @StoreId";
			await ExecuteAsync(sql, new { StoreId = storeId });
		}
	}
}
