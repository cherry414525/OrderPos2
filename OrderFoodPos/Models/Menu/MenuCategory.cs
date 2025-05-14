using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Menu
{
	public class MenuCategory
	{
		public int Id { get; set; } // 類別主鍵
		public int StoreId { get; set; } // 所屬商店
		public string Name { get; set; } // 類別名稱
		public DateTime CreatedDate { get; set; } // 建立時間
		public DateTime? UpdatedDate { get; set; } // 更新時間
		public int? Sequence { get; set; } // 排序編號
	}

	public class GetCategoryRequest
	{
		public int StoreId { get; set; }
	}

	public class UpdateCategoryDto
	{
		public int Id { get; set; } // 雖然 URL 有 id，但前端還是會帶來
		public string Name { get; set; }
		public int StoreId { get; set; }
		public int Sequence { get; set; }
	}
}
