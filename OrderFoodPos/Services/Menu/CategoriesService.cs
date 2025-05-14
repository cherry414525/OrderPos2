using OrderFoodPos.Models.Menu;
using OrderFoodPos.Repositories.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Menu
{
	public class CategoriesService
	{
		private readonly CategoriesRepository _repository;

		public CategoriesService(CategoriesRepository repository)
		{
			_repository = repository;
		}

		public Task<IEnumerable<MenuCategory>> GetAllAsync(int storeId)
		{
			return _repository.GetAllByStoreIdAsync(storeId);
		}

		public Task<int> CreateAsync(MenuCategory category)
		{
			category.CreatedDate = DateTime.Now;
			category.UpdatedDate = DateTime.Now;
			return _repository.AddCategoryAsync(category);
		}

		/// <summary>
		/// 更新分類（名稱、商店、順序）
		/// </summary>
		public Task<int> UpdateCategoryAsync(int id, string name, int storeId, int sequence)
		{
			return _repository.UpdateCategoryAsync(id, name, storeId, sequence); // 傳遞完整參數
		}


		public Task<int> DeleteAsync(int id)
		{
			return _repository.DeleteCategoryAsync(id);
		}

		public async Task<IEnumerable<MenuCategory>> GetCategoriesByStoreIdAsync(int storeId)
		{
			return await _repository.GetCategoriesByStoreIdAsync(storeId);
		}
	}
}
