using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Repositories.Member;
using OrderFoodPos.Services.Member;

namespace OrderFoodPos.Controllers.Member
{
	public class StoreAccountFunction
	{
		private readonly ILogger<StoreAccountFunction> _logger;
		private readonly StoreAccountRepository _storeaccountRepository;
		private readonly StoreAccountService _memberService;
		private readonly EmployeesRepository _employeesRepository;

		public StoreAccountFunction(ILogger<StoreAccountFunction> logger, StoreAccountRepository storeaccountRepository, StoreAccountService memberService, EmployeesRepository employeesRepository)
		{
			_logger = logger;
			_storeaccountRepository = storeaccountRepository;
			_memberService = memberService;
			_employeesRepository = employeesRepository;
			_employeesRepository = employeesRepository;
		}

		[Function("LoginFunction")]
		public async Task<HttpResponseData> Run(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData req,
	FunctionContext context)
		{
			var logger = context.GetLogger("LoginFunction");
			logger.LogInformation("Login 被呼叫");

			var response = req.CreateResponse();

			try
			{
				string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				var data = JsonConvert.DeserializeObject<LoginRequest>(requestBody);

				if (data == null || string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
				{
					response.StatusCode = HttpStatusCode.BadRequest;
					await response.WriteAsJsonAsync(new { message = "請輸入帳號與密碼" });
					return response;
				}

				//改呼叫含完整驗證與記錄的 Service 方法
				var result = await _memberService.LoginAsync(data);

				if (!result.Success)
				{
					if (result.Message == "此帳號已被鎖定")
					{
						response.StatusCode = HttpStatusCode.Forbidden; //  403 Forbidden
					}
					else
					{
						response.StatusCode = HttpStatusCode.Unauthorized; // 其他錯誤維持 401
					}

					await response.WriteAsJsonAsync(new { message = result.Message });
					return response;
				}

				response.StatusCode = HttpStatusCode.OK;
				await response.WriteAsJsonAsync(new { message = result.Message, token = result.Token });
				return response;
			}
			catch (Exception ex)
			{
				logger.LogError($"LoginFunction 發生錯誤：{ex.Message}\n{ex.StackTrace}");
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "伺服器內部錯誤", error = ex.Message });
				return response;
			}
		}



		[Function("RegisterFunction")]

		public async Task<HttpResponseData> Register(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "register")] HttpRequestData req)
		{
			_logger.LogInformation("Register 被呼叫");

			string body = await new StreamReader(req.Body).ReadToEndAsync();
			var data = JsonConvert.DeserializeObject<RegisterRequest>(body);

			var response = req.CreateResponse();

			if (data == null ||
				string.IsNullOrWhiteSpace(data.Username) ||
				string.IsNullOrWhiteSpace(data.Password) ||
				string.IsNullOrWhiteSpace(data.Email) ||
				string.IsNullOrWhiteSpace(data.FullName))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "所有欄位都必填" });
				return response;
			}

			var service = new StoreAccountService(_storeaccountRepository, _employeesRepository);


			RegisterStatus status = await service.RegisterUserAsync(data.Username, data.Password, data.Email, data.FullName);

			switch (status)
			{
				case RegisterStatus.UsernameExists:
					response.StatusCode = HttpStatusCode.Conflict;
					await response.WriteAsJsonAsync(new { message = "帳號已存在" });
					break;

				case RegisterStatus.EmailExists:
					response.StatusCode = HttpStatusCode.Conflict;
					await response.WriteAsJsonAsync(new { message = "信箱已存在" });
					break;

				case RegisterStatus.Failed:
					response.StatusCode = HttpStatusCode.InternalServerError;
					await response.WriteAsJsonAsync(new { message = "註冊失敗，請稍後再試" });
					break;

				case RegisterStatus.Success:
				default:
					response.StatusCode = HttpStatusCode.OK;
					await response.WriteAsJsonAsync(new
					{
						success = true,
						message = "註冊成功"
					});
					break;
			}

			return response;
		}

		//獲取全部的會員資料
		[Function("GetAllMembers")]
		public async Task<HttpResponseData> GetAllMembers(
	[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "members/all")] HttpRequestData req)
		{
			_logger.LogInformation("GetAllMembers 被呼叫");

			var response = req.CreateResponse();

			try
			{
				// 透過 Repository 取得所有會員資料
				var members = await _storeaccountRepository.GetAllMembersAsync();

				response.StatusCode = HttpStatusCode.OK;
				await response.WriteAsJsonAsync(members);
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"取得所有會員時發生錯誤: {ex.Message}");
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "讀取會員失敗，請稍後再試。" });
				return response;
			}
		}

		[Function("CreateMember")]
		public async Task<HttpResponseData> CreateMember(
	[HttpTrigger(AuthorizationLevel.Function, "post", Route = "members/create")] HttpRequestData req)
		{
			var response = req.CreateResponse();

			try
			{
				var body = await new StreamReader(req.Body).ReadToEndAsync();
				var model = JsonConvert.DeserializeObject<StoreAccountModel>(body);

				if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.PasswordHash))
				{
					response.StatusCode = HttpStatusCode.BadRequest;
					await response.WriteAsJsonAsync(new { message = "帳號與密碼為必填" });
					return response;
				}

				var result = await _memberService.CreateUserAsync(model);

				if (!result.Success)
				{
					response.StatusCode = HttpStatusCode.Conflict;
					await response.WriteAsJsonAsync(new { message = result.Message });
				}
				else
				{
					response.StatusCode = HttpStatusCode.OK;
					await response.WriteAsJsonAsync(new { message = "帳號建立成功" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"CreateMember 發生錯誤：{ex.Message}");
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "伺服器錯誤", error = ex.Message });
			}

			return response;
		}





		[Function("UpdateMember")]
		public async Task<HttpResponseData> UpdateMember(
	[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "members/update")] HttpRequestData req)
		{
			_logger.LogInformation("UpdateMember 被呼叫");

			var response = req.CreateResponse();

			try
			{
				var body = await new StreamReader(req.Body).ReadToEndAsync();
				var updateData = JsonConvert.DeserializeObject<StoreAccountModel>(body);

				if (updateData == null || string.IsNullOrWhiteSpace(updateData.Username))
				{
					response.StatusCode = HttpStatusCode.BadRequest;
					await response.WriteAsJsonAsync(new { message = "請提供有效的會員資料" });
					return response;
				}

				// 呼叫 Service 更新會員
				bool updated = await _memberService.UpdateMemberAsync(updateData);

				if (!updated)
				{
					response.StatusCode = HttpStatusCode.NotFound;
					await response.WriteAsJsonAsync(new { message = "更新失敗，查無此會員" });
					return response;
				}

				response.StatusCode = HttpStatusCode.OK;
				await response.WriteAsJsonAsync(new { message = "會員更新成功" });
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"UpdateMember 發生錯誤：{ex.Message}");
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "伺服器錯誤", error = ex.Message });
				return response;
			}
		}

		[Function("GetMemberById")]
		public async Task<HttpResponseData> GetMemberById(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "members/{id:int}")] HttpRequestData req,
			FunctionContext executionContext,
			int id)
		{
			var logger = executionContext.GetLogger("GetMemberById");


			var memberRepo = executionContext.InstanceServices.GetService(typeof(StoreAccountRepository)) as StoreAccountRepository;

			var member = await memberRepo.GetMemberByIdAsync(id);

			var res = req.CreateResponse();

			if (member != null)
			{
				res.StatusCode = HttpStatusCode.OK;
				await res.WriteAsJsonAsync(member); // 回傳找到的會員資料
			}
			else
			{
				res.StatusCode = HttpStatusCode.NotFound;
				await res.WriteAsJsonAsync(new { message = "查無此會員" }); // 查無會員時回傳提示訊息
			}

			return res;

		}



		//取得每日營業時間
		[Function("GetBusinessHours")]
		public async Task<HttpResponseData> GetBusinessHours(
				[HttpTrigger(AuthorizationLevel.Function, "get", Route = "store/{storeId}/businesshours")] HttpRequestData req,
				int storeId) // 使用 Path 取得 storeId
		{
			_logger.LogInformation($"GetBusinessHours 被呼叫，StoreId: {storeId}");

			var response = req.CreateResponse();

			try
			{
				// service的 GetBusinessHours 方法
				var businessHours = await _memberService.GetBusinessHoursAsync(storeId);

				if (businessHours == null)
				{
					response.StatusCode = HttpStatusCode.NotFound;
					await response.WriteAsJsonAsync(new { message = "找不到該商店的營業時間" });
					return response;
				}

				response.StatusCode = HttpStatusCode.OK;
				await response.WriteAsJsonAsync(businessHours); // 回傳營業時間
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"GetBusinessHours 發生錯誤：{ex.Message}\n{ex.StackTrace}");
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "伺服器內部錯誤", error = ex.Message });
				return response;
			}
		}


        //更新每日營業時間
        [Function("UpdateBusinessHours")]
        public async Task<HttpResponseData> UpdateBusinessHours(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "store/{storeId}/businesshours")] HttpRequestData req,
    int storeId)
        {
            _logger.LogInformation($"UpdateBusinessHours 被呼叫，StoreId: {storeId}");

            var response = req.CreateResponse();

            try
            {
                var businessHours = await req.ReadFromJsonAsync<List<StoreBusinessHourModel>>();

                if (businessHours == null || businessHours.Count != 7)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { message = "請提供一週七天的營業時間資料（共 7 筆）" });
                    return response;
                }

                // 檢查所有 StoreId 是否一致，避免錯誤資料
                if (businessHours.Any(h => h.StoreId != storeId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { message = "所有資料的 StoreId 必須一致並與路徑中的 storeId 相同" });
                    return response;
                }

                await _memberService.UpsertWeeklyBusinessHoursAsync(storeId, businessHours);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new { message = "營業時間已成功更新" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateBusinessHours 發生錯誤：{ex.Message}\n{ex.StackTrace}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new { message = "伺服器錯誤", error = ex.Message });
                return response;
            }
        }


		//取得特殊假日
        [Function("GetHolidays")]
        public async Task<HttpResponseData> GetHolidays(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "store/{storeId}/holiday")] HttpRequestData req,
    int storeId)
        {
            var response = req.CreateResponse();

            try
            {
                var data = await _memberService.GetHolidaysAsync(storeId);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetHolidays 發生錯誤：{ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new { message = "伺服器錯誤", error = ex.Message });
            }

            return response;
        }


		//更新特殊假日
        [Function("UpdateHolidays")]
        public async Task<HttpResponseData> UpdateHolidays(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "store/{storeId}/holiday")] HttpRequestData req,
    int storeId)
        {
            var response = req.CreateResponse();

            try
            {
                var requestBody = await req.ReadFromJsonAsync<IEnumerable<HolidayModel>>();

                if (requestBody == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new { message = "無效的請求內容" });
                    return response;
                }

                await _memberService.UpdateHolidaysAsync(storeId, requestBody);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new { message = "更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateHolidays 發生錯誤：{ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new { message = "伺服器錯誤", error = ex.Message });
            }

            return response;
        }



    }

}