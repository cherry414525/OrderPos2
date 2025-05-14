using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Menu
{
	public class MenuItem
    {
		public int? Id { get; set; }
		public int StoreId { get; set; }             // 商店 ID
		public int CategoryId { get; set; }          // 類別 ID
		public string Name { get; set; }             // 餐點名稱
		public string? Description { get; set; }     // 描述（可選）
		public decimal Price { get; set; }           // 價格
		public string? ImageUrl { get; set; }        // 圖片連結（可選）
		public int Sequence { get; set; } = 0;       // 排序
		public int Meter { get; set; } = 0;          // 份量
		public int Stock { get; set; } = 0;          // 庫存
	}


	public class ImportMenuItemDto
	{
		public string Name { get; set; }
		public decimal Price { get; set; }
		public string CategoryName { get; set; }
		public int Stock { get; set; }
		public int Sequence { get; set; }
		public int Meter { get; set; }
	}



	public class ImportMenuRequest
	{
		public int StoreId { get; set; }
		public bool Overwrite { get; set; }
		public List<MenuCategory> Categories { get; set; } = new();
		public List<ImportMenuItemDto> MenuItems { get; set; } = new();
	}


}
