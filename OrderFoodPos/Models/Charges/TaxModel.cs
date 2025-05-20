using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Charges
{
    public class TaxModel
    {
        public int StoreId { get; set; }                  // 商店 ID
        public int TaxRate { get; set; }                  // 稅率（以百分比儲存，例如 5 表示 5%）
        public bool TaxIncluded { get; set; }             // 是否內含稅
        public string RoundingType { get; set; } = "";    // 稅額四捨五入方式
        public int TaxFreeThreshold { get; set; }         // 免稅門檻（例如滿 $100 才收稅）
    }
}
