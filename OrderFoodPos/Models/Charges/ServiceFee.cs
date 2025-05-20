using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Charges
{
    public class ServiceFeeModel
    {
        public int StoreId { get; set; }               // 商店 ID
        public int ServiceFeeRate { get; set; }        // 服務費率（例如 10 表示 10%）
        public bool FeeIncluded { get; set; }          // 是否內含服務費（0 = 否，1 = 是）
        
    }
}
