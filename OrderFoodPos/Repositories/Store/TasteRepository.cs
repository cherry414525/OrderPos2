using Dapper;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using OrderFoodPos.Models.Store;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Store
{
	public class TasteRepository : BaseRepository<AddGroupWithItemsDto>
	{
		public TasteRepository(IDbConnection db) : base(db) { }

		public async Task<IEnumerable<AddGroupWithItemsDto>> GetAddGroupsWithItemsAsync(int storeId)
		{
			string sql = @"
SELECT 
    g.Id AS GroupId, g.GroupName,
    i.Id AS ItemId, i.ItemName, i.Price, NULL AS ImageUrl
FROM StoreAddGroups g
LEFT JOIN StoreAddItems i ON g.Id = i.GroupId
WHERE g.StoreId = @StoreId
ORDER BY g.Id, i.Sequence";

			var groupDict = new Dictionary<int, AddGroupWithItemsDto>();

			var result = await _dbConnection.QueryAsync<AddGroupWithItemsDto, AddItemDto, AddGroupWithItemsDto>(
				sql,
				(group, item) =>
				{
					if (!groupDict.TryGetValue(group.GroupId, out var groupEntry))
					{
						groupEntry = group;
						groupEntry.Items = new List<AddItemDto>();
						groupDict.Add(group.GroupId, groupEntry);
					}

					if (item != null)
						groupEntry.Items.Add(item);

					return groupEntry;
				},
				new { StoreId = storeId },
				splitOn: "ItemId"
			);

			return groupDict.Values;
		}

		//取得餐點類別與口味關聯
		public async Task<IEnumerable<CategoryAddGroupMappingDto>> GetCategoryAddonMappingsAsync(int storeId)
		{
			var sql = @"
        SELECT CategoryId, AddGroupId
        FROM CategoryAddGroupMappings
        WHERE StoreId = @StoreId";

			var result = await _dbConnection.QueryAsync<CategoryAddGroupMappingRaw>(sql, new { StoreId = storeId });

			// 將一對多關係 group by 組合成 List<CategoryAddGroupMappingDto>
			var grouped = result
				.GroupBy(x => x.CategoryId)
				.Select(g => new CategoryAddGroupMappingDto
				{
					StoreId = storeId,
					CategoryId = g.Key,
					AddGroupIds = g.Select(x => x.AddGroupId).ToList()
				});

			return grouped;
		}




		//新增單一口味群組
		public async Task<int> CreateAddGroupAsync(AddGroupDto dto)
		{
			string sql = @"
	INSERT INTO StoreAddGroups (StoreId, GroupName, CreatedDate)
	VALUES (@StoreId, @GroupName, GETDATE());
	SELECT CAST(SCOPE_IDENTITY() as int);"; // 回傳新增 ID

			var newId = await _dbConnection.ExecuteScalarAsync<int>(sql, dto); // 執行插入並取得 ID
			return newId;
		}


		//新增單一口味
		public async Task<int> CreateAddonItemAsync(AddItemCreateDto dto)
		{
			string sql = @"
        INSERT INTO StoreAddItems (GroupId, ItemName, Price, CreatedDate)
        VALUES (@GroupId, @ItemName, @Price, GETDATE());
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

			int newId = await _dbConnection.ExecuteScalarAsync<int>(sql, dto);
			return newId;
		}



		//新增整單口味
		public async Task<int> CreateOrderFlavorAsync(OrderFlavorDto flavor)
		{
			string sql = @"
INSERT INTO StoreTasteOptions (StoreId, OptionName, IsEnabled)
VALUES (@StoreId, @OptionName, @IsEnabled);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

			return await _dbConnection.ExecuteScalarAsync<int>(sql, flavor);
		}


		//獲得整單口味
		public async Task<IEnumerable<OrderFlavorDto>> GetOrderFlavorsAsync(int storeId)
		{
			string sql = "SELECT Id, OptionName, IsEnabled FROM StoreTasteOptions WHERE StoreId = @StoreId";
			return await _dbConnection.QueryAsync<OrderFlavorDto>(sql, new { StoreId = storeId });
		}

		//設定口味與商品類別關聯
		public async Task UpdateCategoryAddonMappingAsync(CategoryAddGroupMappingDto dto)
		{
			// 改用 base class 的 BeginTransactionAsync 方法，確保開啟連線
			using var tran = await BeginTransactionAsync();

			try
			{
				// 先刪除原有的對應
				await _dbConnection.ExecuteAsync(
					"DELETE FROM CategoryAddGroupMappings WHERE StoreId = @StoreId AND CategoryId = @CategoryId",
					new { dto.StoreId, dto.CategoryId },
					transaction: tran);

				// 再新增目前的對應
				foreach (var groupId in dto.AddGroupIds)
				{
					await _dbConnection.ExecuteAsync(
						@"INSERT INTO CategoryAddGroupMappings (StoreId, CategoryId, AddGroupId, CreatedDate)
				  VALUES (@StoreId, @CategoryId, @AddGroupId, GETDATE())",
						new { dto.StoreId, dto.CategoryId, AddGroupId = groupId },
						transaction: tran);
				}

				tran.Commit(); //提交交易
			}
			catch
			{
				tran.Rollback(); //發生錯誤就回滾
				throw;
			}
		}





		//更新整單口味狀態
		public async Task UpdateOrderFlavorStatusAsync(int id, bool isEnabled)
		{
			string sql = "UPDATE StoreTasteOptions SET IsEnabled = @IsEnabled WHERE Id = @Id";
			await _dbConnection.ExecuteAsync(sql, new { Id = id, IsEnabled = isEnabled });
		}

		//更新單一口味群組
		public async Task UpdateAddonGroupAsync(int groupId, string groupName)
		{
			string sql = @"
        UPDATE StoreAddGroups
        SET GroupName = @GroupName,
            UpdatedDate = GETDATE()
        WHERE Id = @GroupId";

			await _dbConnection.ExecuteAsync(sql, new { GroupId = groupId, GroupName = groupName });
		}




		//刪除整單口味
		public async Task<bool> DeleteOrderFlavorAsync(int flavorId)
		{
			string sql = "DELETE FROM StoreTasteOptions WHERE Id = @Id";
			var result = await _dbConnection.ExecuteAsync(sql, new { Id = flavorId });
			return result > 0; // 回傳是否有成功刪除
		}


		//刪除單一口味群組
		public async Task DeleteAddGroupWithItemsAsync(int groupId)
		{
			string sql = @"
    BEGIN TRANSACTION;

    DELETE FROM StoreAddItems WHERE GroupId = @GroupId; -- 先刪子項目
    DELETE FROM StoreAddGroups WHERE Id = @GroupId;     -- 再刪群組

    COMMIT;
    ";

			await _dbConnection.ExecuteAsync(sql, new { GroupId = groupId });
		}

		//刪除單一口味
		public async Task<bool> DeleteAddonItemAsync(int itemId)
		{
			string sql = "DELETE FROM StoreAddItems WHERE Id = @Id";
			var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = itemId });
			return affectedRows > 0;
		}





	}
}
