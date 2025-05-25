using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Member
{
	class Customer
	{
	}


	public class FirebaseLoginDto
	{
		public string IdToken { get; set; }
		public string? Ip { get; set; }
		public string? UserAgent { get; set; }
	}



	public class StoreCustomer
	{
		public int Id { get; set; }
		public int StoreId { get; set; }
		public string Name { get; set; }
		public string PhoneNumber { get; set; }
		public string LineId { get; set; }
		public string Email { get; set; }
		public bool Locked { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
	}

	public class LineProfile
	{
		public string userId { get; set; }
		public string displayName { get; set; }
		public string pictureUrl { get; set; }
		public string statusMessage { get; set; }
	}



	public class LineCodeLoginDto
	{
		public string Code { get; set; }
		public string RedirectUri { get; set; }
		public string State { get; set; }
		public string Ip { get; set; }
		public string? UserAgent { get; set; }
	}

	public class StoreCustomerLoginLog
	{
		public int CustomerId { get; set; }
		public string LoginType { get; set; }
		public DateTime LoginTime { get; set; }
		public string IpAddress { get; set; }
		public string UserAgent { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }
	}





}
