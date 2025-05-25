using Microsoft.AspNetCore.Builder.Extensions;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Repositories.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.ViewEngines;



namespace OrderFoodPos.Services.Member
{
	public class CustomerService
	{
		private readonly CustomerRepository _repository;

		public CustomerService(CustomerRepository repository)
		{
			_repository = repository;

			if (FirebaseApp.DefaultInstance == null)
			{
				// 從環境變數取得 JSON 檔案路徑
				var path = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");

				if (string.IsNullOrWhiteSpace(path))
					throw new Exception("找不到 Firebase 金鑰路徑的環境變數 FIREBASE_CREDENTIAL_PATH");

				FirebaseApp.Create(new AppOptions()
				{
					Credential = GoogleCredential.FromFile(path)
				});
			}

		}

		//電話簡訊登入
		public async Task<string> LoginOrCreateAsync(FirebaseLoginDto dto)
		{
			StoreCustomer customer = null;
			var isNew = false;

			try
			{
				var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto.IdToken);
				var phone = decoded.Claims["phone_number"]?.ToString();

				if (string.IsNullOrEmpty(phone))
					throw new Exception("Token中沒有 phone_number");

				customer = await _repository.GetByPhoneAsync(phone);

				if (customer == null)
				{
					var newCustomer = new StoreCustomer
					{
						PhoneNumber = phone,
						Name = $"用戶{phone.Substring(phone.Length - 4)}",
						Locked = false,
						CreatedDate = DateTime.UtcNow,
						UpdatedDate = DateTime.UtcNow
					};

					await _repository.CreateAsync(newCustomer);
					customer = await _repository.GetByPhoneAsync(phone);
					isNew = true;
				}

				// 🔧 修正：確保 IP 和 UserAgent 正確傳遞
				Console.WriteLine($"DEBUG: 接收到的 IP = '{dto.Ip}'");
				Console.WriteLine($"DEBUG: 接收到的 UserAgent = '{dto.UserAgent}'");

				// 寫入成功登入紀錄
				var loginLog = new StoreCustomerLoginLog
				{
					CustomerId = customer.Id,
					LoginType = "PHONE",
					LoginTime = DateTime.UtcNow,
					IpAddress = dto.Ip ?? "未知IP",          // 🔧 添加預設值
					UserAgent = dto.UserAgent ?? "未知瀏覽器", // 🔧 添加預設值
					Success = true,
					Message = isNew ? "首次登入" : "登入成功"
				};

				Console.WriteLine($"DEBUG: 準備插入的登入記錄 = {System.Text.Json.JsonSerializer.Serialize(loginLog)}");

				await _repository.CreateLogAsync(loginLog);

				// 🔧 修正：使用正確的 JWT Helper 方法和客戶對象
				var token = JwtHelper.GenerateCustomerToken(customer);

				return token;
			}
			catch (Exception ex)
			{
				// 🔧 失敗時也記錄 IP
				var failureLog = new StoreCustomerLoginLog
				{
					CustomerId = customer?.Id ?? 0,
					LoginType = "PHONE",
					LoginTime = DateTime.UtcNow,
					IpAddress = dto.Ip ?? "未知IP",
					UserAgent = dto.UserAgent ?? "未知瀏覽器",
					Success = false,
					Message = $"登入失敗：{ex.Message}"
				};

				Console.WriteLine($"DEBUG: 失敗記錄 = {System.Text.Json.JsonSerializer.Serialize(failureLog)}");
				await _repository.CreateLogAsync(failureLog);

				throw;
			}
		}




		//LINE登入
		public async Task<string> LoginOrCreateWithLineCodeAsync(LineCodeLoginDto dto)
		{
			using var client = new HttpClient();

			var form = new Dictionary<string, string>
	{
		{ "grant_type", "authorization_code" },
		{ "code", dto.Code },
		{ "redirect_uri", dto.RedirectUri },
		{ "client_id", Environment.GetEnvironmentVariable("LINE_CLIENT_ID")! },
		{ "client_secret", Environment.GetEnvironmentVariable("LINE_CLIENT_SECRET")! }
	};

			var tokenResponse = await client.PostAsync("https://api.line.me/oauth2/v2.1/token", new FormUrlEncodedContent(form));
			if (!tokenResponse.IsSuccessStatusCode)
				throw new Exception("LINE token 換取失敗");

			var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
			var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

			var idToken = tokenData.GetProperty("id_token").GetString();
			if (string.IsNullOrWhiteSpace(idToken))
				throw new Exception("找不到 id_token");

			// 解碼 id_token
			var payload = idToken.Split('.')[1];
			var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
			var claims = JsonSerializer.Deserialize<JsonElement>(json);

			var lineId = claims.GetProperty("sub").GetString();
			var name = claims.GetProperty("name").GetString();
			var email = claims.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "";

			if (string.IsNullOrWhiteSpace(lineId))
				throw new Exception("LINE 使用者 ID 為空");

			var customer = await _repository.GetByLineIdAsync(lineId);
			var isNew = false;
			if (customer == null)
			{
				var newCustomer = new StoreCustomer
				{
					LineId = lineId,
					Name = name ?? "LINE 用戶",
					Email = email ?? "",
					PhoneNumber = "",
					Locked = false,
					CreatedDate = DateTime.UtcNow,
					UpdatedDate = DateTime.UtcNow
				};

				await _repository.CreateAsync(newCustomer);
				customer = await _repository.GetByLineIdAsync(lineId);
				isNew = true;
			}
			// 寫入登入紀錄
			await _repository.CreateLogAsync(new StoreCustomerLoginLog
			{
				CustomerId = customer.Id,
				LoginType = "LINE",
				LoginTime = DateTime.UtcNow,
				IpAddress = dto.Ip ?? "",
				UserAgent = dto.UserAgent ?? "",
				Success = true,
				Message = isNew ? "首次登入" : "登入成功"
			});


			// 🔧 修正：使用正確的 JWT Helper 方法和客戶對象
			var token = JwtHelper.GenerateCustomerToken(customer);

			return token;
		}


		// Base64Url 解碼
		private static byte[] Base64UrlDecode(string input)
		{
			string base64 = input.Replace('-', '+').Replace('_', '/');
			switch (base64.Length % 4)
			{
				case 2: base64 += "=="; break;
				case 3: base64 += "="; break;
				case 0: break;
				default: throw new FormatException("無效的 Base64Url 字串");
			}
			return Convert.FromBase64String(base64);
		}
	}
}
