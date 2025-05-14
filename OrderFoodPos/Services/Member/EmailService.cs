using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Member
{
	/// <summary>
	/// 負責 Email 發送的服務
	/// </summary>
	public class EmailService
	{
		private readonly string _smtpServer;
		private readonly int _smtpPort;
		private readonly string _smtpUsername;
		private readonly string _smtpPassword;
		private readonly string _fromEmail;

		public EmailService()
		{
			// **請根據你的 SMTP 設定修改這些參數**
			_smtpServer = "smtp.gmail.com"; // Gmail SMTP 伺服器
			_smtpPort = 587; // TLS 使用 587，SSL 使用 465
			_smtpUsername = "orderfood0129@gmail.com"; // 你的 Gmail 地址
			_smtpPassword = "djbn klfp kiys oxwk"; // 你的 Gmail 應用程式密碼
			_fromEmail = "orderfood0129@gmail.com"; // 發送信件的 Email
		}

		/// <summary>
		/// 發送 Email
		/// </summary>
		public async Task SendEmailAsync(string toEmail, string subject, string body)
		{
			try
			{
				using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
				{
					smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
					smtpClient.EnableSsl = true; // 使用 TLS 加密

					using (var mailMessage = new MailMessage())
					{
						// 設定發件人名稱
						mailMessage.From = new MailAddress(_fromEmail, "歐得富"); // 修改此處新增發件人名稱
						mailMessage.To.Add(toEmail);
						mailMessage.Subject = subject;
						mailMessage.Body = body;
						mailMessage.IsBodyHtml = true; // 設定為 HTML 格式

						await smtpClient.SendMailAsync(mailMessage);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Email 發送失敗: {ex.Message}");
				throw;
			}
		}
	}
}
