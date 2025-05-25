using OrderFoodPos.Models.Store;
using OrderFoodPos.Repositories.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Store
{
	public class TasteService
	{
		private readonly TasteRepository _repository;

		public TasteService(TasteRepository repository)
		{
			_repository = repository;
		}

		public async Task<IEnumerable<AddGroupWithItemsDto>> GetAddGroupsAsync(int storeId)
		{
			return await _repository.GetAddGroupsWithItemsAsync(storeId);
		}

		//取得餐點類別與口味關聯
		public async Task<IEnumerable<CategoryAddGroupMappingDto>> GetCategoryAddonMappingsAsync(int storeId)
		{
			return await _repository.GetCategoryAddonMappingsAsync(storeId);
		}



		//新增單一口味群組
		public async Task<int> CreateAddGroupAsync(AddGroupDto dto)
		{
			return await _repository.CreateAddGroupAsync(dto); // 呼叫 repo 層建立
		}


		//新增整單口味
		public async Task<int> CreateOrderFlavorAsync(OrderFlavorDto flavor)
		{
			return await _repository.CreateOrderFlavorAsync(flavor);
		}

		//新增單一口味
		public async Task<int> CreateAddonItemAsync(AddItemCreateDto dto)
		{
			return await _repository.CreateAddonItemAsync(dto);
		}


		//獲得整單口味
		public async Task<IEnumerable<OrderFlavorDto>> GetOrderFlavorsAsync(int storeId)
		{
			return await _repository.GetOrderFlavorsAsync(storeId);
		}

		//設定口味與商品關聯
		public async Task UpdateCategoryAddonMappingAsync(CategoryAddGroupMappingDto dto)
		{
			await _repository.UpdateCategoryAddonMappingAsync(dto);
		}


		//更新整單口味狀態
		public async Task UpdateOrderFlavorStatusAsync(int id, bool isEnabled)
		{
			await _repository.UpdateOrderFlavorStatusAsync(id, isEnabled);
		}


		//更新單一口味群組
		public async Task UpdateAddonGroupAsync(int groupId, string groupName)
		{
			await _repository.UpdateAddonGroupAsync(groupId, groupName);
		}


		//刪除整單口味
		public async Task<bool> DeleteOrderFlavorAsync(int flavorId)
		{
			return await _repository.DeleteOrderFlavorAsync(flavorId);
		}

		//刪除單一口味群組
		public async Task DeleteAddGroupWithItemsAsync(int groupId)
		{
			await _repository.DeleteAddGroupWithItemsAsync(groupId); // 呼叫 Repository
		}

		//刪除單一加料項目
		public async Task<bool> DeleteAddonItemAsync(int itemId)
		{
			return await _repository.DeleteAddonItemAsync(itemId);
		}

	}
}
