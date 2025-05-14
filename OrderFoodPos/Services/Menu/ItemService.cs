using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OrderFoodPos.Models.Menu;
using OrderFoodPos.Repositories.Menu;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Menu
{
	public class ItemService
	{
		private readonly ItemRepository _repository;
		private readonly BlobServiceClient _blobServiceClient;
		private readonly string _containerName = "images"; // 確保此容器名稱存在

		public ItemService(ItemRepository repository, BlobServiceClient blobServiceClient)
		{
			_repository = repository;
			_blobServiceClient = blobServiceClient;
		}

		/// <summary>
		/// 取得單一餐點資料
		/// </summary>
		public Task<MenuItem?> GetByIdAsync(int id)
		{
			return _repository.GetByIdAsync(id);
		}

		/// <summary>
		/// 取得指定商店的所有餐點資料
		/// </summary>
		public async Task<IEnumerable<MenuItem>> GetAllByStoreIdAsync(int StoreId)
		{
			return await _repository.GetAllByStoreIdAsync(StoreId);
		}

		/// <summary>
		/// 生成用於上傳圖片的 SAS 令牌和 URL
		/// </summary>
		/// <param name="storeId">商店 ID (用於組織文件路徑)</param>
		/// <returns>包含 SAS URL 和 Blob 名稱的對象</returns>
		public async Task<UploadSasInfo> GenerateUploadSasToken(int storeId)
		{
			try
			{
				// 確保容器存在
				var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
				await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

				// 生成唯一的 Blob 名稱 (使用商店 ID 作為文件夾結構)
				var fileName = $"{storeId}/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid()}";
				var blobClient = containerClient.GetBlobClient(fileName);

				// 創建 SAS 令牌，有效期 15 分鐘，僅允許寫入操作
				var sasBuilder = new BlobSasBuilder
				{
					BlobContainerName = _containerName,
					BlobName = fileName,
					Resource = "b", // 'b' 表示 Blob
					ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
				};

				// 設置權限 (只允許上傳)
				sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

				// 獲取 SAS 令牌
				var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

				// 返回 Blob URL 與 SAS 令牌和 Blob 名稱
				return new UploadSasInfo
				{
					SasUrl = blobClient.Uri + sasToken,
					BlobUrl = blobClient.Uri.ToString(),
					BlobName = fileName
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"生成 SAS 令牌時發生錯誤: {ex.Message}");
				throw;
			}
		}

		/// <summary>
		/// 新增餐點，使用已上傳的 Blob URL
		/// </summary>
		public async Task<int> AddAsync(MenuItem model)
		{
			// 確保 ImageUrl 是有效的
			if (!string.IsNullOrEmpty(model.ImageUrl) && !model.ImageUrl.StartsWith("http"))
			{
				// 如果只提供了 Blob 名稱，轉換為完整 URL
				var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
				var blobClient = containerClient.GetBlobClient(model.ImageUrl);
				model.ImageUrl = blobClient.Uri.ToString();
			}

			// 寫入資料庫
			return await _repository.AddMenuItemAsync(model);
		}

		/// <summary>
		/// 批次新增菜單項目
		/// </summary>
		public async Task ImportMenuItemsAsync(int storeId, List<MenuItem> items)
		{
			foreach (var item in items)
			{
				item.StoreId = storeId;
				await _repository.AddMenuItemAsync(item); // 呼叫資料庫層
			}
		}



		/// <summary>
		/// 刪除某家店所有的菜單項目
		/// </summary>
		public async Task DeleteAllMenuItemsByStoreIdAsync(int storeId)
		{
			await _repository.DeleteAllMenuItemsByStoreIdAsync(storeId);
		}

		public async Task DeleteAllCategoriesAndItemsByStoreAsync(int storeId)
		{
			await _repository.DeleteAllItemsByStoreIdAsync(storeId);
			await _repository.DeleteAllCategoriesByStoreIdAsync(storeId);
		}

		public async Task CreateCategoryAsync(MenuCategory category)
		{
			await _repository.CreateCategoryAsync(category);
		}

		public async Task<int> CreateAsync(MenuCategory category)
		{
			return await _repository.CreateCategoryAsync(category); // 假設你有這個方法
		}

		public async Task<int> CreateMenuItemAsync(MenuItem model)
		{
			return await _repository.AddMenuItemAsync(model); // 呼叫你已經有的新增餐點方法
		}

		public async Task DeleteMenuItemAsync(int id)
		{
			await _repository.DeleteMenuItemAsync(id); // 呼叫 Repository 層實作刪除
		}




	}

	/// <summary>
	/// 返回給前端的上傳信息
	/// </summary>
	public class UploadSasInfo
	{
		/// <summary>
		/// 包含 SAS 令牌的完整上傳 URL
		/// </summary>
		public string SasUrl { get; set; }

		/// <summary>
		/// 不含 SAS 的 Blob URL (用於存儲在資料庫)
		/// </summary>
		public string BlobUrl { get; set; }

		/// <summary>
		/// Blob 名稱 (用於引用)
		/// </summary>
		public string BlobName { get; set; }
	}
}