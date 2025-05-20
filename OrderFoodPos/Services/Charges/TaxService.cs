using OrderFoodPos.Models.Charges;
using OrderFoodPos.Repositories.Charges;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Charges
{
    public class TaxService
    {
        private readonly TaxRepository _repository;

        public TaxService(TaxRepository repository)
        {
            _repository = repository;
        }

        
        /// 取得指定商店的稅率設定
        public async Task<TaxModel?> GetTaxByStoreId(int storeId)
        {
            return await _repository.GetByStoreIdAsync(storeId);
        }

        public async Task UpsertTaxSettingAsync(TaxModel model)
        {
            if (model == null || model.StoreId <= 0)
                throw new ArgumentException("TaxModel 資料無效");

            // 先查詢資料是否存在
            var existing = await _repository.GetByStoreIdAsync(model.StoreId);

            if (existing == null)
            {
                await _repository.InsertTaxSettingAsync(model); // 新增
            }
            else
            {
                await _repository.UpdateTaxSettingAsync(model); // 更新
            }
        }

    }
}
