using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Payments
{
    public class JkoPayRequest
    {
        public string orderId { get; set; }
        public int amount { get; set; }
        public string currency { get; set; } = "TWD";
        public string notifyUrl { get; set; }
        public string returnUrl { get; set; }
        public string description { get; set; }
        // 其他街口要求的欄位
    }

}
