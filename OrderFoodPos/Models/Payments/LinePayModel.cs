using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OrderFoodPos.Models
{
    public class LinePayRequest
    {
        public int amount { get; set; }
        public string currency { get; set; } = "TWD";
        public string orderId { get; set; }
        public string packageName { get; set; }
        public List<Package> packages { get; set; } = new();
        public RedirectUrls redirectUrls { get; set; }
    }

    public class Package
    {
        public string id { get; set; }
        public int amount { get; set; }
        public string name { get; set; }
        public List<Product> products { get; set; }
    }

    public class Product
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int price { get; set; }
    }

    public class RedirectUrls
    {
        public string confirmUrl { get; set; }
        public string cancelUrl { get; set; }
    }

    public class LinePayRefundRequest
    {
        public string transactionId { get; set; }
        public int refundAmount { get; set; }
    }

    public class LinePayQueryRequest
    {
        public string transactionId { get; set; }
        public string orderId { get; set; }
    }

    public class StoreLinePay
    {
        public string StoreID { get; set; }
        public string ChannelId { get; set; }
        public string ChannelSecret { get; set; }
    }


    }
