using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Orders
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public DateTime OrderTime { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string MenuName { get; set; }  
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class Payment
    {
        public string OrderId { get; set; } = default!;
        public string TransactionId { get; set; } = default!;
        public DateTime PayTime { get; set; }
        public int Amount { get; set; }
        public string Method { get; set; } = "LinePay";
        public string Status { get; set; } = "Paid";
    }
}
