using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Payments
{
    public class JkoPayRequest
    {
        public string MerchantID { get; set; }
        public string StoreID { get; set; }
        public string StoreName { get; set; }
        public string GatewayTradeNo { get; set; } = "";
        public string MerchantTradeNo { get; set; }
        public string PosID { get; set; }
        public string PosTradeTime { get; set; } // yyyy/MM/dd HH:mm:ss
        public string CardToken { get; set; }
        public int TradeAmount { get; set; }
        public int UnRedeem { get; set; }
        public string Remark { get; set; } = "";
        public string Extra1 { get; set; } = "";
        public string Extra2 { get; set; } = "";
        public string Extra3 { get; set; } = "";
        public string SendTime { get; set; } // yyyyMMddHHmmss
        public string Sign { get; set; } // 自動產生
    }

    public class JkoPayCancelRequest
    {
        public string MerchantID { get; set; }
        public string StoreID { get; set; }
        public string StoreName { get; set; }
        public string GatewayTradeNo { get; set; }
        public string MerchantTradeNo { get; set; }
        public string PosID { get; set; }
        public string PosTradeTime { get; set; }
        public string CardToken { get; set; }
        public int TradeAmount { get; set; }
        public int UnRedeem { get; set; }
        public string Remark { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public string SendTime { get; set; }
        public string Sign { get; set; }
    }

}
