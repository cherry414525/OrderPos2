using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Store
{
    class Taste
    {
    }

	public class AddItemDto
	{
		public int ItemId { get; set; } // 加料項目 ID
		public string ItemName { get; set; } = string.Empty; // 加料名稱
		public decimal Price { get; set; } // 價格
	}

	public class AddGroupWithItemsDto // 原本有 Items 的保留
	{
		public int GroupId { get; set; } // 群組 ID
		public string GroupName { get; set; } = string.Empty; // 群組名稱
		public List<AddItemDto> Items { get; set; } = new(); // 對應項目清單
	}


	public class OrderFlavorDto
	{
		public int Id { get; set; }
		public int StoreId { get; set; }
		public string OptionName { get; set; } = string.Empty;
		public bool IsEnabled { get; set; }
	}

	public class UpdateFlavorStatusDto
	{
		public bool IsEnabled { get; set; }
	}

	public class AddGroupDto
	{
		public int StoreId { get; set; } // 店家 ID
		public string GroupName { get; set; } = string.Empty; // 群組名稱
	}

	public class AddItemCreateDto
	{
		public int GroupId { get; set; } // 加料群組 ID
		public string ItemName { get; set; } = string.Empty; // 選項名稱
		public decimal Price { get; set; } // 價格
	}

	public class AddGroupUpdateDto
	{
		public int Id { get; set; } // 加料群組 ID
		public string GroupName { get; set; } = string.Empty; // 新名稱
	}


	public class CategoryAddGroupMappingDto
	{
		public int CategoryId { get; set; } // 類別 ID
		public int StoreId { get; set; } // 店家 ID
		public List<int> AddGroupIds { get; set; } = new(); // 多個加料群組 ID
	}



	public class CategoryAddGroupMappingRaw
	{
		public int CategoryId { get; set; }
		public int AddGroupId { get; set; }
	}


}
