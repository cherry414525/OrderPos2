using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


namespace OrderFoodPos
{
	public class SqlConnectionFactory
	{
		private readonly IConfiguration _configuration;

        private readonly string _connectionString;

        public string GetConnectionString() => _connectionString;
        public SqlConnectionFactory(IConfiguration configuration)
		{
			_configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

		public IDbConnection CreateConnection()
		{
			//  優先抓環境變數或 local.settings.json
			var connectionString = _configuration.GetConnectionString("DefaultConnection");

			if (string.IsNullOrWhiteSpace(connectionString))
			{
				throw new InvalidOperationException("無法取得資料庫連線字串，請確認 local.settings.json 或 Azure 設定");
			}

			Console.WriteLine("資料庫連線字串取得成功：" + connectionString); // Debug 訊息
			return new SqlConnection(connectionString);
		}
	}

}
