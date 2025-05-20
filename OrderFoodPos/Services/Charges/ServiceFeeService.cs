using OrderFoodPos.Models.Charges;
using OrderFoodPos.Repositories.Charges;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Charges
{
    public class ServiceFeeService
    {
        private readonly ServiceFeeRepository _repository;

        public ServiceFeeService(ServiceFeeRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 取得指定商店的服務費設定
        /// </summary>
        public async Task<ServiceFeeModel?> GetServiceFeeByStoreId(int storeId)
        {
            return await _repository.GetByStoreIdAsync(storeId);
        }

        /// <summary>
        /// 新增或更新服務費設定
        /// </summary>
        public async Task UpsertServiceFeeSettingAsync(ServiceFeeModel model)
        {
            if (model == null || model.StoreId <= 0)
                throw new ArgumentException("ServiceFeeModel 資料無效");

            // 先查詢資料是否存在
            var existing = await _repository.GetByStoreIdAsync(model.StoreId);

            if (existing == null)
            {
                await _repository.InsertServiceFeeSettingAsync(model); // 新增
            }
            else
            {
                await _repository.UpdateServiceFeeSettingAsync(model); // 更新
            }
        }
    }
}
