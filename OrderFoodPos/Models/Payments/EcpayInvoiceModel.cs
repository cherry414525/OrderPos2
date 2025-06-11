using System.Collections.Generic;

public class InvoiceRequest
{
    public string RelateNumber { get; set; }
    public string CustomerID { get; set; }
    public string CustomerIdentifier { get; set; }  // ✅ 統編
    public string CustomerName { get; set; }        // ✅ 客戶名稱（必要）
    public string CustomerAddr { get; set; }        // ✅ 地址
    public string CustomerPhone { get; set; }
    public string CustomerEmail { get; set; }

    public string ClearanceMark { get; set; }
    public string Print { get; set; }               // ✅ Print = 1 時 CustomerName 必填
    public string Donation { get; set; }
    public string LoveCode { get; set; }
    public string CarrierType { get; set; }
    public string CarrierNum { get; set; }

    public string TaxType { get; set; }
    public int SalesAmount { get; set; }
    public string InvoiceRemark { get; set; }
    public string InvType { get; set; }
    public string vat { get; set; }

    public List<InvoiceItem> Items { get; set; }    // ✅ 改成 "Items"，跟你 JSON 對齊
}


public class InvoiceItem
{
    public int ItemSeq { get; set; }
    public string ItemName { get; set; }
    public int ItemCount { get; set; }
    public string ItemWord { get; set; }
    public int ItemPrice { get; set; }
    public string ItemTaxType { get; set; }
    public int ItemAmount { get; set; }
    public string ItemRemark { get; set; }
}
