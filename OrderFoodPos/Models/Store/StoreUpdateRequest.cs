using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Store
{
	public class StoreUpdateRequest
	{
		public int Id { get; set; }
		public string StoreName { get; set; }
		public string Address { get; set; }
		public string PhoneNumber { get; set; }
		public string Description { get; set; }
		public bool Opened { get; set; }
		public string BusinessType { get; set; }
		public string Image { get; set; }
		public DateTime? UpdatedDate { get; set; } = DateTime.Now;
		public string Facebook { get; set; }
		public string Instagram { get; set; }
		public string Line { get; set; }
	}

	public class StoreDetailModel
	{
		public int Id { get; set; }
		public int StoreAccountsId { get; set; }
		public string StoreName { get; set; }
		public string Address { get; set; }
		public string PhoneNumber { get; set; }
		public string Description { get; set; }
		public string BusinessType { get; set; }
		public string Plan { get; set; }
		public string Star { get; set; }
		public string Image { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? UpdatedDate { get; set; }
		public DateTime? ExpirationDate { get; set; }
		public string Facebook { get; set; }
		public string Instagram { get; set; }
		public string Line { get; set; }
		public bool Opened { get; set; }
		public string? TaxID { get; set; }
	}

	public class GetStoreRequest
	{
		public int StoreId { get; set; }
	}



}
